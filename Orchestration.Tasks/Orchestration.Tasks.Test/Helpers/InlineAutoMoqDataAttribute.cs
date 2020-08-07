using AutoFixture.Xunit2;

namespace Orchestration.Tasks.Test.Helpers
{
    public class InlineAutoMoqDataAttribute : InlineAutoDataAttribute
    {

        public InlineAutoMoqDataAttribute(params object[] objects)
            : base(new AutoMoqDataAttribute(), objects)
        {
        }

    }
}
