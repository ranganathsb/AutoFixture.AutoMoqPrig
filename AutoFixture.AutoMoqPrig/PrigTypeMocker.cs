/* 
 * File: PrigTypeMocker.cs
 * 
 * Author: Akira Sugiura (urasandesu@gmail.com)
 * 
 * 
 * Copyright (c) 2016 Akira Sugiura
 *  
 *  This software is MIT License.
 *  
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the "Software"), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *  
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *  
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 *  THE SOFTWARE.
 */


using Moq;
using Moq.Language;
using Ploeh.AutoFixture.Kernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Urasandesu.AutoFixture.AutoMoqPrig.Mixins.System.Reflection;
using Urasandesu.Moq.Prig;
using Urasandesu.Prig.Framework;

namespace Urasandesu.AutoFixture.AutoMoqPrig
{
    public class PrigTypeMocker : ISpecimenBuilder
    {
        readonly MockStorage m_ms;
        readonly ISpecimenBuilder m_builder;

        public PrigTypeMocker(MockStorage ms, ISpecimenBuilder builder)
        {
            if (ms == null)
                throw new ArgumentNullException("ms");

            if (builder == null)
                throw new ArgumentNullException("builder");

            m_ms = ms;
            m_builder = builder;
        }

        public object Create(object request, ISpecimenContext context)
        {
            if (context == null)
                using (InstanceGetters.DisableProcessing())
                    throw new ArgumentNullException("context");

            var result = m_builder.Create(request, context);
            var specimen = default(PrigTypeSpecimen);
            if ((specimen = result as PrigTypeSpecimen) != null)
                return CreateMock(specimen, context);
            else if (!(result is NoSpecimen))
                return result;

            using (InstanceGetters.DisableProcessing())
                return new NoSpecimen();
        }

        object CreateMock(PrigTypeSpecimen specimen, ISpecimenContext context)
        {
            foreach (var stub in specimen.Stubs)
                SetAutoBody(stub, context);
            return specimen.Object;
        }

        void SetAutoBody(IndirectionStubSpecimen stub, ISpecimenContext context)
        {
            var proxy = default(MockProxy);
            var dlgtType = default(Type);
            var dlgtMethod = default(MethodInfo);
            using (InstanceGetters.DisableProcessing())
            {
                dlgtType = stub.IndirectionDelegate;
                dlgtMethod = stub.IndirectionDelegateMethod;
            }
            proxy = GetAutoMock(dlgtType, dlgtMethod, context);
            stub.SetBody(proxy.Object);

            using (InstanceGetters.DisableProcessing())
                m_ms.Assign(stub.CallableTarget, proxy);
        }

        const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;

        const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

        MockProxy GetAutoMock(Type dlgtType, MethodInfo dlgtMethod, ISpecimenContext context)
        {
            Debug.Assert(dlgtType.IsSubclassOf(typeof(Delegate)));
            var proxy = default(MockProxy);
            using (InstanceGetters.DisableProcessing())
            {
                var createMethod = m_ms.GetType().GetMethod("Create", Type.EmptyTypes);
                var createMethodInst = createMethod.MakeGenericMethod(dlgtType);
                proxy = (MockProxy)createMethodInst.Invoke(m_ms, null);
            }
            DoSetupForAutoMock(proxy, dlgtMethod, context);
            return proxy;
        }

        void DoSetupForAutoMock(MockProxy proxy, MethodInfo dlgtMethod, ISpecimenContext context)
        {
            var setup = default(object);
            using (InstanceGetters.DisableProcessing())
            {
                var proxyType = proxy.GetType();
                var setupMethod = GetSetupMethod(proxyType, dlgtMethod);
                var expression = GetExpression(setupMethod.GetParameters()[0].ParameterType, dlgtMethod, context);
                setup = setupMethod.Invoke(proxy, new object[] { expression });
            }
            DoReturnsForAutoMock(setup, context);
        }

        MethodInfo GetSetupMethod(Type mockType, MethodInfo dlgtMethod)
        {
            var setupMethods = mockType.GetMethods(PublicInstance).Where(_ => _.Name == "Setup");
            if (dlgtMethod.ReturnType == typeof(void))
                return GetActionSetupMethod(setupMethods);
            else
                return GetFuncSetupMethod(setupMethods, dlgtMethod.ReturnType);
        }

        MethodInfo GetActionSetupMethod(IEnumerable<MethodInfo> setupMethods)
        {
            return setupMethods.First(_ => _.GetParameters().ContainsInGenericArguments(typeof(Action<>)));
        }

        MethodInfo GetFuncSetupMethod(IEnumerable<MethodInfo> setupMethods, Type resultType)
        {
            return setupMethods.First(_ => _.GetParameters().ContainsInGenericArguments(typeof(Func<,>))).MakeGenericMethod(resultType);
        }

        Expression GetExpression(Type setupParamType, MethodInfo dlgtMethod, ISpecimenContext context)
        {
            var lambdaMethod = typeof(Expression).GetMethods(PublicStatic).
                                                  Where(_ => _.Name == "Lambda").
                                                  Where(_ => _.IsGenericMethod).
                                                  Where(_ => _.GetParameters().Length == 2).
                                                  Single(_ => _.GetParameters()[1].ParameterType.IsArray);
            var lambdaType = setupParamType.GetGenericArguments()[0];
            var dlgtType = lambdaType.GetGenericArguments()[0];
            var lambdaMethodInst = lambdaMethod.MakeGenericMethod(lambdaType);
            var paramExp = Expression.Parameter(dlgtType, "_");
            var anyAccedptableParamExps = GetAnyAccedptableParameterExpressions(dlgtMethod, context);
            var bodyExp = Expression.Invoke(paramExp, anyAccedptableParamExps);
            var paramExps = new[] { paramExp };
            return (Expression)lambdaMethodInst.Invoke(null, new object[] { bodyExp, paramExps });
        }

        IEnumerable<Expression> GetAnyAccedptableParameterExpressions(MethodInfo dlgtMethod, ISpecimenContext context)
        {
            return dlgtMethod.GetParameters().Select(_ => ToAnyAccedptableParameterExpression(_, context));
        }

        Expression ToAnyAccedptableParameterExpression(ParameterInfo paramInfo, ISpecimenContext context)
        {
            var paramType = paramInfo.ParameterType;
            if (paramType.IsByRef)
                return ToAnyAccedptableRefParameterExpression(paramType, context);
            else
                return ToAnyAccedptableParameterExpression(paramType);
        }

        Expression ToAnyAccedptableRefParameterExpression(Type type, ISpecimenContext context)
        {
            var valType = type.GetElementType();
            var value = context.Resolve(valType);
            return Expression.Constant(value, valType);
        }

        Expression ToAnyAccedptableParameterExpression(Type type)
        {
            var isAnyMethod = typeof(It).GetMethod("IsAny");
            var isAnyMethodInst = isAnyMethod.MakeGenericMethod(type);
            return Expression.Call(null, isAnyMethodInst);
        }

        void DoReturnsForAutoMock(object setup, ISpecimenContext context)
        {
            var setupType = default(Type);
            var resultType = default(Type);
            using (InstanceGetters.DisableProcessing())
            {
                setupType = setup.GetType();
                if (!setupType.GetInterfaces().Where(_ => _.IsGenericType).Any(_ => _.GetGenericTypeDefinition() == typeof(IReturns<,>)))
                    return;

                resultType = setupType.GetGenericArguments()[1];
            }
            var result = context.Resolve(resultType);
            if (result is NoSpecimen)
                return;

            using (InstanceGetters.DisableProcessing())
            {
                var returnsMethod = setupType.GetMethods(PublicInstance).Single(ReturnsImmediateValue);
                resultType = result.GetType();  // replace result type because perhaps it is a Prig Type for a specified instance.
                var paramType = returnsMethod.GetParameters()[0].ParameterType;
                if (!paramType.IsAssignableFrom(resultType))
                {
                    var opImplicitMethod = resultType.GetMethods(PublicStatic).
                                                      Where(_ => _.Name == "op_Implicit").
                                                      SingleOrDefault(_ => _.ReturnType == paramType);
                    if (opImplicitMethod == null)
                        throw new InvalidOperationException(string.Format("{0} doesn't have implicit conversion to {1}.", resultType, paramType));

                    result = opImplicitMethod.Invoke(null, new object[] { result });
                }
                returnsMethod.Invoke(setup, new object[] { result });
            }
        }

        bool ReturnsImmediateValue(MethodInfo method)
        {
            if (method.Name != "Returns")
                return false;

            return !method.GetParameters().Any(_ => typeof(Delegate).IsAssignableFrom(_.ParameterType));
        }
    }
}
