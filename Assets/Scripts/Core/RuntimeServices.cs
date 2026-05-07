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
        if (Services.TryGetValue(typeof(T), out registration) && registration.Service != null)
        {
            service = (T)registration.Service;
            return true;
        }

        service = null;
        return false;
    }

    public static T GetOrCreate<T>(ServiceLifetime lifetime) where T : Component
    {
        T service;
        if (TryGet(out service))
        {
            return service;
        }

        service = UnityEngine.Object.FindObjectOfType<T>();
        if (service != null)
        {
            Register(service, lifetime);
            return service;
        }

        return new GameObject(typeof(T).Name).AddComponent<T>();
    }

    public static bool Register<T>(T service, ServiceLifetime lifetime) where T : UnityEngine.Object
    {
        if (service == null)
        {
            return false;
        }

        var serviceType = typeof(T);
        Registration existing;
        if (Services.TryGetValue(serviceType, out existing) && existing.Service != null && existing.Service != service)
        {
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

    public static void ResetSceneServices()
    {
        var sceneServiceTypes = new List<Type>();

        foreach (var service in Services)
        {
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
                UnityEngine.Object.Destroy(component.gameObject);
            }
            else
            {
                UnityEngine.Object.Destroy(registration.Service);
            }
        }
    }
}
