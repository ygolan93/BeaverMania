using System;
using System.Collections.Generic;
using UnityEngine;

public static class RuntimeServices
{
    private sealed class Registration
    {
        public UnityEngine.Object Service;
        public ServiceLifetime Lifetime;
    }

    private static readonly Dictionary<Type, Registration> Services = new Dictionary<Type, Registration>();

    public static bool TryGet<T>(out T service) where T : UnityEngine.Object
    {
        Registration registration;
        if (Services.TryGetValue(typeof(T), out registration))
        {
            if (registration.Service != null)
            {
                service = (T)registration.Service;
                return true;
            }

            Services.Remove(typeof(T));
            BuildSafeLogger.InfoOnce(typeof(T).FullName + ".RemovedStaleService", "Removed stale runtime service registration: " + typeof(T).Name + ".");
        }

        service = null;
        return false;
    }

    public static bool TryGetOrFind<T>(ServiceLifetime lifetime, out T service) where T : Component
    {
        if (TryGet(out service))
        {
            return true;
        }

        service = UnityEngine.Object.FindObjectOfType<T>();
        if (service != null)
        {
            Register(service, lifetime);
            return true;
        }

        return false;
    }

    public static T GetRequired<T>(ServiceLifetime lifetime) where T : Component
    {
        T service;
        if (TryGetOrFind(lifetime, out service))
        {
            return service;
        }

        throw MissingService<T>(lifetime, false);
    }

    public static T GetOrCreate<T>(ServiceLifetime lifetime) where T : Component
    {
        T service;
        if (TryGetOrFind(lifetime, out service))
        {
            return service;
        }

        if (lifetime == ServiceLifetime.Persistent)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return CreateFallback<T>(lifetime);
#else
            throw MissingService<T>(lifetime, true);
#endif
        }

        return CreateFallback<T>(lifetime);
    }

    private static T CreateFallback<T>(ServiceLifetime lifetime) where T : Component
    {
        var serviceType = typeof(T);
        BuildSafeLogger.WarnOnce(
            serviceType.FullName + ".MissingService",
            "Missing runtime service; creating fallback " + serviceType.Name + ".",
            null,
            serviceType.Name);
        var owner = new GameObject(serviceType.Name);
        owner.AddComponent<RuntimeServiceOwnerMarker>().MarkFallbackCreated();
        var service = owner.AddComponent<T>();
        Register(service, lifetime);
        return service;
    }

    private static InvalidOperationException MissingService<T>(ServiceLifetime lifetime, bool blockedFallback) where T : Component
    {
        var serviceType = typeof(T);
        var message = "Missing " + lifetime + " runtime service: " + serviceType.Name + ". Add it to the scene/bootstrap before use.";
        if (blockedFallback)
        {
            message += " Release builds do not create fallback persistent services.";
        }

        Debug.LogError(message);
        return new InvalidOperationException(message);
    }

    public static bool Register<T>(T service, ServiceLifetime lifetime) where T : UnityEngine.Object
    {
        if (service == null)
        {
            return false;
        }

        var serviceType = typeof(T);
        Registration existing;
        if (Services.TryGetValue(serviceType, out existing) && existing.Service == null)
        {
            Services.Remove(serviceType);
            BuildSafeLogger.InfoOnce(serviceType.FullName + ".RemovedStaleService", "Removed stale runtime service registration: " + serviceType.Name + ".");
        }

        if (Services.TryGetValue(serviceType, out existing) && existing.Service != null && existing.Service != service)
        {
            BuildSafeLogger.WarnOnce(
                serviceType.FullName + ".DuplicateManager",
                "Duplicate manager/service rejected: " + serviceType.Name + ".",
                service as UnityEngine.Object,
                serviceType.Name);
            var component = service as Component;
            if (component != null)
            {
                UnityEngine.Object.Destroy(component.gameObject);
            }
            else
            {
                UnityEngine.Object.Destroy(service);
            }

            return false;
        }

        Services[serviceType] = new Registration
        {
            Service = service,
            Lifetime = lifetime
        };

        var serviceComponent = service as Component;
        if (lifetime == ServiceLifetime.Persistent && serviceComponent != null)
        {
            UnityEngine.Object.DontDestroyOnLoad(serviceComponent.gameObject);
        }

        BuildSafeLogger.InfoOnce(
            serviceType.FullName + ".Registered." + lifetime,
            "Registered " + lifetime + " runtime service: " + serviceType.Name + ".",
            service as UnityEngine.Object);
        return true;
    }

    public static void Unregister<T>(T service) where T : UnityEngine.Object
    {
        if (service == null)
        {
            return;
        }

        Registration registration;
        if (Services.TryGetValue(typeof(T), out registration) && registration.Service == service)
        {
            Services.Remove(typeof(T));
        }
    }

    public static string GetDiagnostics()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var lines = new List<string>();
        foreach (var service in Services)
        {
            var component = service.Value.Service as Component;
            string scope = component != null && component.gameObject.scene.name == "DontDestroyOnLoad" ? "persistent-object" : "scene-local-object";
            lines.Add(service.Key.Name + " lifetime=" + service.Value.Lifetime + " state=" + (service.Value.Service == null ? "stale" : scope));
        }

        return string.Join("\n", lines.ToArray());
#else
        return string.Empty;
#endif
    }

    public static void ResetSceneServices()
    {
        var sceneServiceTypes = new List<Type>();

        foreach (var service in Services)
        {
            if (service.Value.Service == null)
            {
                sceneServiceTypes.Add(service.Key);
                continue;
            }

            if (service.Value.Lifetime == ServiceLifetime.Scene)
            {
                sceneServiceTypes.Add(service.Key);
            }
        }

        foreach (var serviceType in sceneServiceTypes)
        {
            var registration = Services[serviceType];
            Services.Remove(serviceType);

            if (registration.Service == null)
            {
                continue;
            }

            var component = registration.Service as Component;
            if (component != null)
            {
                var owner = component.GetComponent<RuntimeServiceOwnerMarker>();
                if (owner != null && owner.DestroyOnSceneServiceReset)
                {
                    UnityEngine.Object.Destroy(component.gameObject);
                }

                continue;
            }

            UnityEngine.Object.Destroy(registration.Service);
        }
    }
}
