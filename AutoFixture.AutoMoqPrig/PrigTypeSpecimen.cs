/* 
 * File: PrigTypeInfo.cs
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
using System.Collections.Generic;
using System.Reflection;

namespace Urasandesu.AutoFixture.AutoMoqPrig
{
    public class PrigTypeSpecimen
    {
        public PrigTypeSpecimen(object obj)
        {
            Object = obj;
        }
        public object Object { get; private set; }

        List<IndirectionStubSpecimen> m_stubs = new List<IndirectionStubSpecimen>();
        public IEnumerable<IndirectionStubSpecimen> Stubs { get { return m_stubs; } }

        public IndirectionStubSpecimen CreateStub(MethodInfo target)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            var preparable = target.Invoke(Object, null);
            var preparableType = preparable.GetType();
            var bodyProp = preparableType.GetProperty("Body");
            var indDlgt = bodyProp.PropertyType;
            var indDlgtMethod = indDlgt.GetMethod("Invoke");
            return new IndirectionStubSpecimen(Object, target, preparable, bodyProp, indDlgt, indDlgtMethod);
        }

        public void AddStub(IndirectionStubSpecimen stub)
        {
            if (stub == null)
                throw new ArgumentNullException("stub");

            m_stubs.Add(stub);
        }
    }
}
