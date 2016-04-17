/* 
 * File: PrigTypeIndirectionStub.cs
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


using System;
using System.Reflection;
using Urasandesu.Prig.Framework;

namespace Urasandesu.AutoFixture.AutoMoqPrig
{
    public class IndirectionStubSpecimen
    {
        internal IndirectionStubSpecimen(object obj, MethodInfo target, object bodyPropTarget, PropertyInfo bodyProp, Type indDlgt, MethodInfo indDlgtMethod)
        {
            Target = target;
            BodyPropertyTarget = bodyPropTarget;
            BodyProperty = bodyProp;
            IndirectionDelegate = indDlgt;
            {
                var funcType = typeof(Func<>);
                var funcTypeInst = funcType.MakeGenericType(Target.ReturnType);
                CallableTarget = Delegate.CreateDelegate(funcTypeInst, obj, Target);
            }
            IndirectionDelegateMethod = indDlgtMethod;
        }

        public Delegate CallableTarget { get; private set; }
        public MethodInfo Target { get; private set; }
        public object BodyPropertyTarget { get; private set; }
        public PropertyInfo BodyProperty { get; private set; }
        public Type IndirectionDelegate { get; private set; }
        public MethodInfo IndirectionDelegateMethod { get; private set; }

        public void SetBody(object value)
        {
            if (InstanceGetters.IsDisabledProcessing())
                throw new InvalidOperationException("'SetBody(object)' can't be called while Prig framework is disabling processing. " + 
                                                    "Confirm unintended calls for 'InstanceGetters.DisableProcessing().'");
            
            BodyProperty.SetValue(BodyPropertyTarget, value, null);
        }
    }
}
