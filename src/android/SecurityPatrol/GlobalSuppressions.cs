// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// Suppress StyleCop warnings
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1633:File should have header", Justification = "Header is defined in source generation")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1200:Using directives should be placed correctly", Justification = "Using directives are placed outside namespace as per project convention")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:Field names should not begin with underscore", Justification = "Underscore prefix is used for private fields as per project convention")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:Prefix local calls with this", Justification = "This prefix is not used as per project convention")]

// Suppress Microsoft Design warnings
[assembly: SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "General exception handling is required for resilience in mobile applications")]
[assembly: SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", Justification = "Argument validation is handled through nullable reference types")]

// Suppress Microsoft Naming warnings for test projects
[assembly: SuppressMessage("Microsoft.Naming", "CA1707:IdentifiersShouldNotContainUnderscores", Justification = "Underscores are used in test method names for readability", Scope = "namespaceanddescendants", Target = "SecurityPatrol.UnitTests")]