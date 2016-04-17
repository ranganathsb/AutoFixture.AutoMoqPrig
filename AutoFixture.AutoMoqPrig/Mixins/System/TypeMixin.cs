/* 
 * File: TypeMixin.cs
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
using System.Linq;

namespace Urasandesu.AutoFixture.AutoMoqPrig.Mixins.System
{
    static class TypeMixin
    {
        public static bool ContainsInGenericArguments(this Type t, Type typeDefinition)
        {
            if (typeDefinition.IsGenericType && !typeDefinition.IsGenericTypeDefinition)
                throw new ArgumentException("The parameter must be a type definition.", "typeDefinition");

            return t.EnumerateGenericArgument().Contains(typeDefinition);
        }

        public static IEnumerable<Type> EnumerateGenericArgument(this Type t)
        {
            if (t.IsGenericType)
                yield return t.GetGenericTypeDefinition();
            else
                yield return t;

            foreach (var genericArg in t.GetGenericArguments())
                foreach (var chidGenericArg in genericArg.EnumerateGenericArgument())
                    yield return chidGenericArg;
        }
    }
}
