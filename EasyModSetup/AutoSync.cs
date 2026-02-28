using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EasyModSetup;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class AutoSync : Attribute
{
    [AutoSync]
    public static string TestStringProp { get => "Hello world!"; set => SimplerPlugin.MOD_NAME = value; }
    public static bool ShouldSync = true;

    public static Action<bool[], int[], float[], string[]> SetSyncedVars;
    private static Dictionary<Type, Func<Array>> syncedVarGetters;
    public static T[] GetSyncedVars<T>() => syncedVarGetters[typeof(T)].Invoke() as T[];

    private static Type[] SupportedTypes => new Type[] { typeof(bool), typeof(int), typeof(float), typeof(string) };
    private static int TypeIdx(Type t) => Array.IndexOf(SupportedTypes, t);

    public static void RegisterSyncedVars()
    {
        try
        {
            var types = Assembly.GetExecutingAssembly().GetTypesSafely();
            var tempFields = types.SelectMany(
                t => t.GetStaticFieldsSafely()
                    .Where(f => f.GetCustomAttribute<AutoSync>() != null)
                );
            var tempProperties = types.SelectMany(
                t => t.GetStaticPropertiesSafely()
                    .Where(p => p.GetCustomAttribute<AutoSync>() != null)
                );

            SimplerPlugin.Log($"Found {tempFields.Count()} auto-sync fields and {tempProperties.Count()} auto-sync properties.");

            if (tempFields.Count() < 1 && tempProperties.Count() < 1)
            {
                ShouldSync = false;
                return;
            }
            ShouldSync = true;

            //make set method
            try
            {
                ParameterExpression[] parameters = SupportedTypes.Select(t => Expression.Parameter(t.MakeArrayType(), t.Name+"Array")).ToArray();
                int[] indexCounters = new int[SupportedTypes.Length];

                BlockExpression expression = Expression.Block(typeof(void),
                    tempFields.Select(
                        f => MakeSetFunc(Expression.Field(null, f), f.FieldType, ref parameters, ref indexCounters, f.DeclaringType)
                    ).Concat(
                        tempProperties.Select(
                            p => MakeSetFunc(Expression.Property(null, p), p.PropertyType, ref parameters, ref indexCounters, p.DeclaringType)
                        )
                    ).Where(e => e != null) //don't include null expressions, obviously
                    .ToArray()
                );

                SetSyncedVars = Expression.Lambda<Action<bool[], int[], float[], string[]>>(expression, parameters).Compile();
            }
            catch (Exception ex) { SimplerPlugin.Error(ex); }

            //make get methods
            try
            {
                syncedVarGetters?.Clear();
                syncedVarGetters ??= new(SupportedTypes.Length);
                foreach (Type t in SupportedTypes)
                {
                    Expression expression = Expression.NewArrayInit(t,
                        tempFields.Select(
                            f => MakeGetFunc(t, Expression.Field(null, f), f.FieldType)
                        ).Concat(
                            tempProperties.Select(
                                p => MakeGetFunc(t, Expression.Property(null, p), p.PropertyType)
                            )
                        ).Where(e => e != null) //don't include null expressions, obviously
                        .ToArray()
                        );

                    syncedVarGetters.Add(t, Expression.Lambda<Func<Array>>(expression).Compile());
                }
            }
            catch (Exception ex) { SimplerPlugin.Error(ex); }

        }
        catch (Exception ex) { SimplerPlugin.Error(ex); }

    }

    private static Expression MakeSetFunc(Expression fieldOrProp, Type type, ref ParameterExpression[] parameters, ref int[] indexCounters, Type declaringType)
    {
        try
        {
            if (type.IsSubclassOf(typeof(ConfigurableBase))) //configs
            {
                Type cType = type.GetGenericArguments()[0];
                string propertyName = nameof(ConfigurableBase.BoxedValue);
                int typeIdx = TypeIdx(typeof(string));
                if (SupportedTypes.Contains(cType))
                {
                    propertyName = "Value";
                    typeIdx = TypeIdx(cType);
                }
                return Expression.Assign(Expression.Property(fieldOrProp, type.GetProperty(propertyName)), Expression.ArrayAccess(parameters[typeIdx], Expression.Constant(indexCounters[typeIdx]++, typeof(int))));
            }
            else if (SupportedTypes.Contains(type))
            {
                //figure out which array we're reading and which counter we're using
                int typeIdx = TypeIdx(type);
                //assign the field with the value of the array (at the current index)
                return Expression.Assign(fieldOrProp, Expression.ArrayAccess(parameters[typeIdx], Expression.Constant(indexCounters[typeIdx]++, typeof(int))));
            }
            else //don't tell user; we will tell him in the get func
                SimplerPlugin.Error($"Unsupported auto-sync type: {type.Name} at {declaringType.FullName}");
        }
        catch (Exception ex) { SimplerPlugin.Error(ex); }
        return null;
    }

    private static Expression MakeGetFunc(Type t, Expression fieldOrProp, Type fType)
    {
        try
        {
            if (fType.IsSubclassOf(typeof(ConfigurableBase))) //configs
            {
                Type cType = fType.GetGenericArguments()[0];
                if (cType == t)
                {
                    return Expression.Property(fieldOrProp, fType.GetProperty("Value"));
                }
                else if (t != typeof(string) || SupportedTypes.Contains(cType)) //only add unsupported configs to strings
                    return null;
                //convert the boxed value to a string
                return Expression.Call(Expression.Property(fieldOrProp, fType.GetProperty(nameof(ConfigurableBase.BoxedValue))), fType.GetMethod(nameof(object.ToString)));
            }
            else if (fType == t)
            {
                return fieldOrProp;
            }
        }
        catch (Exception ex) { SimplerPlugin.Error(ex); }
        return null;
    }

}