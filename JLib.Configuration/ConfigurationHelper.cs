﻿using System.Reflection;
using JLib.DependencyInjection;
using JLib.Exceptions;
using JLib.Helper;
using JLib.ValueTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JLib.Configuration;

public static class ConfigurationHelper
{
    /// <summary>
    /// <para>supports environments using the following behavior:</para>
    /// <code>
    /// {
    ///     Environment: "Dev1",
    ///     SectionA:{
    ///         Environment: "Dev2",
    ///         Dev1:{
    ///             MyValue:"Ignored"
    ///         },
    ///         Dev2:{
    ///             MyValue:"Used"
    ///         },
    ///         MyValue:"Ignored"
    ///     },
    ///     SectionB:{
    ///         Dev1:{
    ///             MyValue:"Used"
    ///         },
    ///         Dev2:{
    ///             MyValue:"Ignored"
    ///         },
    ///         MyValue:"Ignored"
    ///     },
    ///     SectionC:{
    ///         Environment: "",
    ///         Dev1:{
    ///             MyValue:"Ignored"
    ///         },
    ///         Dev2:{
    ///             MyValue:"Ignored"
    ///         },
    ///         MyValue:"Used"
    ///     }
    /// }
    /// </code>
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    // only used to hold the summary above
    private static Ignored BehaviorSummaryHolder =>
        throw new InvalidOperationException("you should not call this property");


    /// <summary>
    /// returns a new instance of the section <typeparamref name="T"/> under the given <paramref name="configSectionName"/>
    /// <br/>does not validate
    /// <br/>does not check whether the section is actually present
    /// <inheritdoc cref="BehaviorSummaryHolder"/>
    /// </summary>
    public static IConfigurationSection GetSection<T>(this IConfiguration config, string configSectionName, ILogger logger)
        where T : class, new()
    {
        // code duplicated in ServiceCollectionHelper.AddAllConfigSections
        var topLevelEnvironment = config[ConfigurationSections.Environment];
        if (topLevelEnvironment != null)
            logger.LogInformation("Loading config for top level environment {environment}", topLevelEnvironment);

        var sectionInstance = config.GetSection(configSectionName);

        var sectionEnvironment = sectionInstance[ConfigurationSections.Environment];

        var environment = sectionEnvironment ?? topLevelEnvironment;
        if (environment is not null)
        {
            var environmentType = sectionEnvironment is not null ? "override" : "topLevel";
            logger.LogInformation(
                "Loading section {environment}.{section} ({sectionType}). Environment is defined in {environmentType}",
                environment, configSectionName, typeof(T).FullClassName(true), environmentType);
            sectionInstance = sectionInstance.GetSection(environment);
        }
        else
            logger.LogInformation("Loading section {section} ({sectionType})", configSectionName,
                typeof(T).FullClassName(true));

        return sectionInstance;
    }

    /// <summary>
    /// extracts the sectionName from the <see cref="ConfigSectionNameAttribute"/> of <typeparamref name="T"/>. if it is not found, a <see cref="InvalidSetupException"/> will be thrown<br/>
    /// <inheritdoc cref="GetSection{T}(IConfiguration,string)"/>
    /// </summary>
    public static IConfigurationSection GetSection<T>(this IConfiguration config, ILogger logger)
        where T : class, new()
    {
        var sectionName = typeof(T).GetCustomAttribute<ConfigSectionNameAttribute>()?.SectionName
                          ?? throw new InvalidSetupException(
                              $"missing {nameof(ConfigSectionNameAttribute)} on class {typeof(T).FullClassName()}");
        return config.GetSection<T>(sectionName.Value, logger);
    }

    /// <summary>
    /// extracts the sectionName from the <see cref="ConfigSectionNameAttribute"/> of <typeparamref name="T"/>. if it is not found, a <see cref="InvalidSetupException"/> will be thrown<br/>
    /// <inheritdoc cref="GetSection{T}(IConfiguration,string)"/>
    /// </summary>
    public static T GetSectionObject<T>(this IConfiguration config, ILogger logger)
        where T : class, new()
    {
        var instance = new T();
        var section = config.GetSection<T>(logger);
        section.Bind(instance);
        return instance;
    }
}