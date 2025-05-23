// EnumUtility.cs
using System;
using UnityEngine; // <--- A�ADE ESTA L�NEA

public static class EnumUtility
{
    public static T Parse<T>(string value) where T : struct
    {
        if (string.IsNullOrEmpty(value))
        {
            // Devuelve el valor por defecto del enum si el string es nulo o vac�o
            // o maneja el error como prefieras.
            // Considera si quieres un log aqu� tambi�n, o simplemente devolver default.
            return default(T);
        }
        if (System.Enum.TryParse<T>(value, true, out T result)) // true para ignorar may�sculas/min�sculas
        {
            return result;
        }
        else
        {
            // El valor no pudo ser parseado, devuelve el valor por defecto o lanza una excepci�n
            Debug.LogError($"EnumUtility: No se pudo parsear '{value}' al enum {typeof(T).Name}. Se devuelve el valor por defecto.");
            return default(T);
        }
    }
}