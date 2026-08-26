using System.Reflection;

public class DebugEntry
{
    public object Target;
    public FieldInfo Field;
    public MethodInfo Method;
    public string FieldName = "";

    public bool IsField => Field != null;
    public bool IsMethod => Method != null;
    
    public void SetFieldValue(string value)
    {
        var field = Field;

        object convertedValue = field.FieldType switch
        {
            var t when t == typeof(int) => int.Parse(value),
            var t when t == typeof(float) => float.Parse(value),
            var t when t == typeof(bool) => bool.Parse(value),
            _ => value
        };

        field.SetValue(Target, convertedValue);
    }

    public object GetValue()
    {
        if (IsField)
        {
            return Field.GetValue(Target);
        }

        if (IsMethod)
        {
            return Method.Name;
        }

        return null;
    }
    
    public void Execute()
    {
        Method.Invoke(Target, null);
    }
    
    public void Execute(string value)
    {
        ParameterInfo parameter =
            Method.GetParameters()[0];

        object convertedValue =
            parameter.ParameterType switch
            {
                var t when t == typeof(int)
                    => int.Parse(value),

                var t when t == typeof(float)
                    => float.Parse(value),

                var t when t == typeof(bool)
                    => bool.Parse(value),

                _ => value
            };

        Method.Invoke(
            Target,
            new[] { convertedValue });
    }
}
