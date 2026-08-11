using System.Windows.Data;

namespace XpEng.Coder03.Views.Converters;

[ValueConversion(typeof(bool), typeof(string))]
public class BooleanToStringConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) {
        //if parameter is not configured properly with pipeline delimiter(s) than return null
        if (parameter == null) return null;
        var returnValues = parameter.ToString()?.Split('|');
        if (returnValues != null && !returnValues.Any()) return null;
        //if input value is null.. 
        if (value == null || value.ToString() == string.Empty) {
            //..and converter parameter is configured for null returns return the null replacement.
            if (returnValues.Count() == 3) return returnValues[2];
            //..otherwise return null
            else return null;
        }
        bool boolValue = false;
        try { boolValue = System.Convert.ToBoolean(value.ToString()); }
        catch (Exception) { boolValue = false; }
        //..use the first pipeline delimiter parameter for true..
        string result;
        if (boolValue) result = returnValues[0];
        //..and the second for false
        else result = returnValues[1];
        return result;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) {
        return Binding.DoNothing;
    }
}