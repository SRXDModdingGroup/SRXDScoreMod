using System;
using System.Linq;
using System.Reflection;

namespace SRXDScoreMod; 

public static class ReflectionUtils {
    public static Action MethodToAction(Type type, string name, BindingFlags bindingAttr = 0)
        => MethodToDelegate<Action>(type, name, new Type[] { }, bindingAttr);
    public static Action<T1> MethodToAction<T1>(Type type, string name, BindingFlags bindingAttr = 0)
        => MethodToDelegate<Action<T1>>(type, name, new [] { typeof(T1) }, bindingAttr);
    public static Action<T1, T2> MethodToAction<T1, T2>(Type type, string name, BindingFlags bindingAttr = 0)
        => MethodToDelegate<Action<T1, T2>>(type, name, new [] { typeof(T1), typeof(T2) }, bindingAttr);
    public static Action<T1, T2, T3> MethodToAction<T1, T2, T3>(Type type, string name, BindingFlags bindingAttr = 0)
        => MethodToDelegate<Action<T1, T2, T3>>(type, name, new [] { typeof(T1), typeof(T2), typeof(T3) }, bindingAttr);
    public static Action<T1, T2, T3, T4> MethodToAction<T1, T2, T3, T4>(Type type, string name, BindingFlags bindingAttr = 0)
        => MethodToDelegate<Action<T1, T2, T3, T4>>(type, name, new [] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, bindingAttr);

    public static Func<TResult> MethodToFunc<TResult>(Type type, string name, BindingFlags bindingAttr = 0)
        => MethodToDelegate<Func<TResult>>(type, name, new Type[] { }, bindingAttr);
    public static Func<T1, TResult> MethodToFunc<T1, TResult>(Type type, string name, BindingFlags bindingAttr = 0)
        => MethodToDelegate<Func<T1, TResult>>(type, name, new [] { typeof(T1) }, bindingAttr);
    public static Func<T1, T2, TResult> MethodToFunc<T1, T2, TResult>(Type type, string name, BindingFlags bindingAttr = 0)
        => MethodToDelegate<Func<T1, T2, TResult>>(type, name, new [] { typeof(T1), typeof(T2) }, bindingAttr);
    public static Func<T1, T2, T3, TResult> MethodToFunc<T1, T2, T3, TResult>(Type type, string name, BindingFlags bindingAttr = 0)
        => MethodToDelegate<Func<T1, T2, T3, TResult>>(type, name, new [] { typeof(T1), typeof(T2), typeof(T3) }, bindingAttr);
    public static Func<T1, T2, T3, T4, TResult> MethodToFunc<T1, T2, T3, T4, TResult>(Type type, string name, BindingFlags bindingAttr = 0)
        => MethodToDelegate<Func<T1, T2, T3, T4, TResult>>(type, name, new [] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) }, bindingAttr);

    private static TDelegate MethodToDelegate<TDelegate>(Type type, string name, Type[] paramTypes, BindingFlags bindingAttr) where TDelegate : Delegate {
        var methodInfo = type.GetMethod(name, bindingAttr, null, paramTypes, null)
                         ?? type.GetMethod(name, bindingAttr, null, paramTypes.Skip(1).ToArray(), null);
        
        return (TDelegate) Delegate.CreateDelegate(typeof(TDelegate), methodInfo);
    }
}