using System;


public class Singleton<T> where T : new()
{
    private static readonly object singletonLockObj = new object ();
    private static T instance;
    public static T Instance
    {
        get
        {
            lock (singletonLockObj)
                {
                    if (instance == null)
                    {
                        instance = new T();
                    }
                    return instance;
                }
        }
    }
}