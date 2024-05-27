using AutoFixture;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace SPTrans.StatusCartaoPersonalizado.Test
{
    public static class FixtureExtension
    {
        public static Mock<T> InjectMoq<T>(this IFixture fixture) where T : class
        {
            var mock = new Mock<T>();
            fixture.Inject(mock);
            return mock;
        }
    }
}
