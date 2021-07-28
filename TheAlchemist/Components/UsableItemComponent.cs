using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TheAlchemist.Components
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ItemUsage
    {
        None,
        Consume,
        Throw
    }

    class UsableItemComponent : Component<UsableItemComponent>
    {
        public List<ItemUsage> Usages { get; set; }
    }
}
