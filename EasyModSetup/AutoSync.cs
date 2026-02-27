using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace EasyModSetup;

//public class AutoStaticVarSync
//{
[AttributeUsage(AttributeTargets.Field)]
public class AutoSync : Attribute
{
    [AutoSync]
    public static Configurable<float> TestConfig = new(123.45f);
    [AutoSync]
    public static Configurable<KeyCode> TestConfig2 = new(KeyCode.KeypadEnter);
    [AutoSync]
    public static float TestFloat1 = 0.1f, TestFloat2 = 12.4f;
    [AutoSync]
    public static int TestInt1 = 5;

    //public static FieldInfo[] SyncedFields;
    //public static FieldInfo[] SyncedConfigs;
    //public static PropertyInfo[] SyncedProperties;
    public static Action<bool[], int[], float[], string[]> SetSyncedVars;
    public static Dictionary<Type, Func<Array>> syncedVarGetters;
    public static bool ShouldSync = true;

    public static T[] GetSyncedVars<T>() => syncedVarGetters[typeof(T)].Invoke() as T[];

    //private static bool IsSupportedType(Type t) => t == typeof(bool) || t == typeof(int) || t == typeof(float) || t == typeof(string);
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

            SimplerPlugin.Log($"Found {tempFields.Count()} auto-sync fields.");

            if (tempFields.Count() < 1)
            {
                ShouldSync = false;
                return;
            }
            ShouldSync = true;

            //make set method
            try
            {
                ParameterExpression[] parameters = SupportedTypes.Select(t => Expression.Parameter(t.MakeArrayType(), t.Name+"Array")).ToArray();
                int[] actualCounters = new int[SupportedTypes.Length];

                BlockExpression expression = Expression.Block(typeof(void),
                    tempFields.Select<FieldInfo, Expression>(
                        f =>
                        {
                            List<Expression> exps = new();
                            try
                            {
                                Type fType = f.FieldType;
                                if (fType.IsSubclassOf(typeof(ConfigurableBase))) //configs
                                {
                                    Type cType = fType.GetGenericArguments()[0];
                                    string propertyName = nameof(ConfigurableBase.BoxedValue);
                                    int typeIdx = TypeIdx(typeof(string));
                                    if (SupportedTypes.Contains(cType))
                                    {
                                        propertyName = "Value";
                                        typeIdx = TypeIdx(cType);
                                    }
                                    return Expression.Assign(Expression.Property(Expression.Field(null, f), fType.GetProperty(propertyName)), Expression.ArrayAccess(parameters[typeIdx], Expression.Constant(actualCounters[typeIdx]++, typeof(int))));
                                }
                                else if (SupportedTypes.Contains(fType))
                                {
                                    //figure out which array we're reading and which counter we're using
                                    int typeIdx = TypeIdx(fType);
                                    //assign the field with the value of the array (at the current index)
                                    return Expression.Assign(Expression.Field(null, f), Expression.ArrayAccess(parameters[typeIdx], Expression.Constant(actualCounters[typeIdx]++, typeof(int))));
                                }
                                else
                                    SimplerPlugin.Error($"Unsupported auto-sync type: {f.FieldType.Name} at {f.DeclaringType.FullName}");
                            } catch (Exception ex) { SimplerPlugin.Error(ex); }
                            return null;
                        }
                    ).Where(e => e != null) //don't include null expressions, obviously
                    .ToArray()
                    );

                string temp = "Expressions: ";
                foreach (var e in expression.Expressions)
                    temp += e.ToString() + ", ";
                SimplerPlugin.Log(temp);

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
                    Expression expression = Expression.NewArrayInit(t, tempFields.Select<FieldInfo, Expression>(
                        f =>
                        {
                            try
                            {
                                Type fType = f.FieldType;
                                if (fType.IsSubclassOf(typeof(ConfigurableBase))) //configs
                                {
                                    Type cType = fType.GetGenericArguments()[0];
                                    //string propertyName = nameof(ConfigurableBase.BoxedValue);
                                    if (cType == t)
                                    {
                                        return Expression.Property(Expression.Field(null, f), fType.GetProperty("Value"));
                                    }
                                    else if (t != typeof(string) || SupportedTypes.Contains(cType)) //only add unsupported configs to strings
                                        return null;
                                    //convert the boxed value to a string
                                    return Expression.Call(Expression.Property(Expression.Field(null, f), fType.GetProperty(nameof(ConfigurableBase.BoxedValue))), fType.GetMethod(nameof(object.ToString)));
                                }
                                else if (fType == t)
                                {
                                    return Expression.Field(null, f);
                                }
                                //else //don't tell the user again; we already said this once
                                    //SimplerPlugin.Error($"Unsupported auto-sync type: {f.FieldType.Name} at {f.DeclaringType.FullName}");
                            }
                            catch (Exception ex) { SimplerPlugin.Error(ex); }
                            return null;
                        }
                        ).Where(e => e != null) //don't include null expressions, obviously
                        .ToArray()
                        );

                    syncedVarGetters.Add(t, Expression.Lambda<Func<Array>>(expression, new ParameterExpression[0]).Compile());
                }

                string fString = "Synced floats: ";
                float[] syncedFloats = GetSyncedVars<float>();
                foreach (float f in syncedFloats)
                    fString += f + ", ";
                SimplerPlugin.Log(fString);

                string sString = "Synced strings: ";
                foreach (string s in GetSyncedVars<string>())
                    sString += s + ", ";
                SimplerPlugin.Log(sString);

                syncedFloats[0] = 567.23f;
                SetSyncedVars(GetSyncedVars<bool>(), GetSyncedVars<int>(), syncedFloats, GetSyncedVars<string>());
                fString = "Synced floats: ";
                foreach (float f in GetSyncedVars<float>())
                    fString += f + ", ";
                SimplerPlugin.Log(fString);
            }
            catch (Exception ex) { SimplerPlugin.Error(ex); }

            //isolate Configurables
            /*SyncedConfigs = tempFields.Where(f => f.FieldType.IsSubclassOf(typeof(ConfigurableBase))).ToArray();
            SyncedFields = tempFields.Except(SyncedConfigs) //don't include configs
                .Where(f =>
                {
                    if (IsSupportedType(f.FieldType)) return true; //it's supported; it's fine
                    SimplerPlugin.Error($"Unsupported auto-sync type: {f.FieldType.Name} at {f.DeclaringType.FullName}");
                    return false;
                }
                ).ToArray(); //everything but configs
            */

            /*SyncedProperties = types.SelectMany(
                t => t.GetStaticPropertiesSafely()
                    .Where(p =>
                    {
                        if (p.GetCustomAttribute<AutoSync>() == null) return false; //
                        if (IsSupportedType(p.PropertyType)) return true; //it's supported; it's fine
                        SimplerPlugin.Error($"Unsupported auto-sync type: {p.PropertyType.Name} at {p.DeclaringType.FullName}");
                        return false;
                    }
                    )
                ).ToArray();*/
        }
        catch (Exception ex) { SimplerPlugin.Error(ex); }

        //SimplerPlugin.Log($"AutoSync found the following: {SyncedFields.Length} fields, {SyncedConfigs.Length} configs");//, {SyncedProperties.Length} properties.");
    }

}