using System;
using UnityEngine;

/// <summary>
///     Attribute that require implementation of the provided interface.
/// </summary>
public class RequireInterfaceAttribute : PropertyAttribute
{
    /// <summary>
    ///     Requiring implementation of the <see cref="T:RequireInterfaceAttribute" /> interface.
    /// </summary>
    /// <param name="type">Interface type.</param>
    public RequireInterfaceAttribute(Type type)
    {
        requiredType = type;
    }

    // Interface type.
    public Type requiredType { get; private set; }
}