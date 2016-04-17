/* 
 * File: PrigTypeCollector.cs
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


using Ploeh.AutoFixture.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Urasandesu.AutoFixture.AutoMoqPrig.Mixins.System.Reflection;
using Urasandesu.Prig.Framework;

namespace Urasandesu.AutoFixture.AutoMoqPrig
{
    public class PrigTypeCollector : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            var requestedType = request as Type;
            if (requestedType == null)
                using (InstanceGetters.DisableProcessing())
                    return new NoSpecimen();

            using (InstanceGetters.DisableProcessing())
            {
                {
                    var result = CreatePrigType(requestedType);
                    if (!(result is NoSpecimen))
                        return result;
                }

                {
                    var result = CreateInstancePrigType(requestedType);
                    if (!(result is NoSpecimen))
                        return result;
                }

                return new NoSpecimen();
            }
        }

        static bool CanBeConfigured(IndirectionStubSpecimen stub)
        {
            var dlgtMethod = stub.IndirectionDelegateMethod;
            return !dlgtMethod.IsGenericMethod &&
                   !dlgtMethod.HasRefParameters() &&
                   (!dlgtMethod.IsVoid() || dlgtMethod.HasOutParameters());
        }

        const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;

        object CreatePrigType(Type type)
        {
            var methods = type.GetMethods(PublicStatic).Where(_ => typeof(IBehaviorPreparable).IsAssignableFrom(_.ReturnType)).ToArray();
            if (methods.Length == 0)
                return new NoSpecimen();

            var obj = default(object);
            var result = new PrigTypeSpecimen(obj);
            foreach (var stub in methods.Where(_ => !_.IsGenericMethod).Select(_ => result.CreateStub(_)).Where(CanBeConfigured))
                result.AddStub(stub);
            return result;
        }

        const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

        object CreateInstancePrigType(Type type)
        {
            type = ReplaceInstancePrigType(type);
            var methods = type.GetMethods(PublicInstance).Where(_ => typeof(IBehaviorPreparable).IsAssignableFrom(_.ReturnType)).ToArray();
            if (methods.Length == 0)
                return new NoSpecimen();

            var obj = Activator.CreateInstance(type);
            var result = new PrigTypeSpecimen(obj);
            foreach (var stub in methods.Where(_ => !_.IsGenericMethod).Select(_ => result.CreateStub(_)).Where(CanBeConfigured))
                result.AddStub(stub);
            return result;
        }

        Type ReplaceInstancePrigType(Type type)
        {
            var instPrigTypeName = string.Format("{0}.Prig.PProxy{1}", type.Namespace, type.Name);
            return GetInstancePrigType(instPrigTypeName) ?? type;
        }

        static Dictionary<string, Type> ms_instPrigTypes;
        static Dictionary<string, Type> InstancePrigTypes
        {
            get
            {
                if (ms_instPrigTypes == null)
                {
                    var repos = new IndirectionAssemblyRepository();
                    var instPrigTypes = repos.FindAll().SelectMany(_ => _.GetTypes()).Where(_ => _.Name.StartsWith("PProxy")).ToDictionary(_ => _.FullName);
                    ms_instPrigTypes = new Dictionary<string, Type>(instPrigTypes);
                }
                return ms_instPrigTypes;
            }
        }
        Type GetInstancePrigType(string fullName)
        {
            var result = default(Type);
            InstancePrigTypes.TryGetValue(fullName, out result);
            return result;
        }
    }
}
