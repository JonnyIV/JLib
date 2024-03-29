﻿using AutoMapper;
using JLib.Reflection;

namespace JLib.AutoMapper;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DisableAutoProfileAttribute : Attribute, IDisableAutoProfileAttribute
{
}

#if NET7_0_OR_GREATER

/// <summary>
/// marks the type as <see cref="TypeValueType.HasCustomAutoMapperProfile"/> = true, which should remove it from all automated profiles and register the <see cref="Profile"/> of <typeparamref name="T"/> to autoMapper
/// </summary>
/// <typeparam name="T"></typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DisableAutoProfileAttribute<T> : Attribute, IDisableAutoProfileAttribute
    where T : Profile
{
    public Type CustomProfile => typeof(T);
}
#endif