
namespace Orchestration.Tasks.Models
{
    public enum ViewLevel : int
    {
        /// <summary>
        /// Logs are internal and shouldn't be exposed to others
        /// </summary>
        Internal = 0, // default level if level is not specified

        /// <summary>
        /// Logs that can be viewed by customer
        /// </summary>
        Customer = 10,

        /// <summary>
        /// All
        /// </summary>
        All = 100,
    }

}
