using UnityEngine;

namespace MyketPlugin
{
    public interface IMyketManagerContainer
    {
        GameObject EnsureRoot();
    }

    public sealed class UnityMyketManagerContainer : IMyketManagerContainer
    {
        private const string RootObjectName = "MyketIABPlugin";

        public GameObject EnsureRoot()
        {
            var root = GameObject.Find(RootObjectName);
            if (root != null)
                return root;

            root = new GameObject(RootObjectName);
            Object.DontDestroyOnLoad(root);
            return root;
        }
    }

    public static class MyketManagerBootstrap
    {
        private static IMyketManagerContainer mContainer;

        public static void Configure(IMyketManagerContainer container)
        {
            mContainer = container;
        }

        public static void Initialize<T>() where T : MonoBehaviour
        {
            Initialize(typeof(T));
        }

        public static void Initialize(System.Type type)
        {
            if (type == null)
                return;

            if (!typeof(MonoBehaviour).IsAssignableFrom(type))
            {
                Debug.LogError("MyketManagerBootstrap.Initialize requires a MonoBehaviour type.");
                return;
            }

            if ((Object.FindObjectOfType(type) as MonoBehaviour) != null)
                return;

            var container = mContainer ?? (mContainer = new UnityMyketManagerContainer());
            var root = container.EnsureRoot();

            var managerObject = new GameObject(type.Name);
            managerObject.transform.SetParent(root.transform, false);
            managerObject.AddComponent(type);
            Object.DontDestroyOnLoad(managerObject);
        }
    }
}
