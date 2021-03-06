﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;
using Ploeh.AutoFixture.Kernel;

namespace Ploeh.AutoFixture.NUnit3
{
    /// <summary>
    /// This attribute acts as a TestCaseAttribute but allow incomplete parameter values, 
    /// which will be provided by AutoFixture. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    [CLSCompliant(false)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Justification = "This attribute is the root of a potential attribute hierarchy.")]
    public class InlineAutoDataAttribute : Attribute, ITestBuilder
    {
        private readonly object[] _existingParameterValues;
        private readonly IFixture _fixture;

        /// <summary>
        /// Construct a <see cref="InlineAutoDataAttribute"/>
        /// with parameter values for test method
        /// </summary>
        public InlineAutoDataAttribute(params object[] arguments)
            : this(new Fixture(), arguments)
        {
        }

        /// <summary>
        /// Construct a <see cref="InlineAutoDataAttribute"/> with an <see cref="IFixture"/> 
        /// and parameter values for test method
        /// </summary>
        protected InlineAutoDataAttribute(IFixture fixture, params object[] arguments)
        {
            if (null == fixture)
            {
                throw new ArgumentNullException(nameof(fixture));
            }

            if (null == arguments)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            this._fixture = fixture;
            this._existingParameterValues = arguments;
        }

        /// <summary>
        /// Gets the parameter values for the test method.
        /// </summary>
        public IEnumerable<object> Arguments
        {
            get { return this._existingParameterValues; }
        }

        /// <summary>
        ///     Construct one or more TestMethods from a given MethodInfo,
        ///     using available parameter data.
        /// </summary>
        /// <param name="method">The MethodInfo for which tests are to be constructed.</param>
        /// <param name="suite">The suite to which the tests will be added.</param>
        /// <returns>One or more TestMethods</returns>
        public IEnumerable<TestMethod> BuildFrom(IMethodInfo method, Test suite)
        {
            var test = new NUnitTestCaseBuilder().BuildTestMethod(method, suite, this.GetParametersForMethod(method));

            yield return test;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This method is always expected to return an instance of the TestCaseParameters class.")]
        private TestCaseParameters GetParametersForMethod(IMethodInfo method)
        {
            try
            {
                var parameters = method.GetParameters();

                var parameterValues = this.GetParameterValues(parameters);

                return new TestCaseParameters(parameterValues.ToArray());
            }
            catch (Exception ex)
            {
                return new TestCaseParameters(ex);
            }
        }

        /// <summary>
        /// Get values for a collection of <see cref="IParameterInfo"/>
        /// </summary>
        private IEnumerable<object> GetParameterValues(IEnumerable<IParameterInfo> parameters)
        {
            return this._existingParameterValues.Concat(this.GetMissingValues(parameters));
        }

        private IEnumerable<object> GetMissingValues(IEnumerable<IParameterInfo> parameters)
        {
            var parametersWithoutValues = parameters.Skip(this._existingParameterValues.Count());

            return parametersWithoutValues.Select(this.GetValueForParameter);
        }

        /// <summary>
        /// Get value for an <see cref="IParameterInfo"/>
        /// </summary>
        private object GetValueForParameter(IParameterInfo parameterInfo)
        {
            CustomizeFixtureByParameter(parameterInfo);

            return new SpecimenContext(this._fixture)
                .Resolve(parameterInfo.ParameterInfo);
        }

        private void CustomizeFixtureByParameter(IParameterInfo parameter)
        {
            var customizeAttributes = parameter.GetCustomAttributes<CustomizeAttribute>(false);
            foreach (var ca in customizeAttributes)
            {
                var customization = ca.GetCustomization(parameter.ParameterInfo);
                this._fixture.Customize(customization);
            }
        }
    }
}