using AutoFixture;
using AutoFixture.AutoMoq;
using System.Linq;

namespace SPTrans.StatusCartaoPersonalizado.Test
{
    public class AutoMoqFixture
    {
        public IFixture Fixture { get; }

        public AutoMoqFixture()
        {
            Fixture = new Fixture().Customize(new AutoMoqCustomization());
            Fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                                .ForEach(b => Fixture.Behaviors.Remove(b));
            Fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        }
    }
}
