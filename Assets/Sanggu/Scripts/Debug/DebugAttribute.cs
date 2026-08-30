using System;

[AttributeUsage(AttributeTargets.Field, Inherited = true)]
public class DebugFieldAttribute : Attribute
{
    public string FieldName;
    public DebugFieldAttribute(string fieldName = null)
    {
        FieldName = fieldName;
    }
}

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class DebugButtonAttribute : Attribute
{
    public string MethodName;
    public DebugButtonAttribute(string methodName = null)
    {
        MethodName = methodName;
    }
}