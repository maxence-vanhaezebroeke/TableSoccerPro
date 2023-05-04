using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component
{
    protected static T _instance = null;
    public static T Instance
    {
        get
        {
            if (_instance == null)
                throw new MissingReferenceException();
            return _instance;
        }
    }

    // If you need to instantiate a new singleton, to replace the old one, use this variable
    // Example : when switching back to Singleton initial scene, with Destroy & Awake order, there can be 2 singleton.
    // So if new one should replace the old one, set this to true. (before the on destroy ofc)
    protected bool _isBeingDestroyed = false;
    public bool IsBeingDestroyed { get { return _isBeingDestroyed; } }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            if ((_instance as Singleton<T>).IsBeingDestroyed == false)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
        }
    }

    protected virtual void OnDestroy() 
    {
        _isBeingDestroyed = true;
    }
}
