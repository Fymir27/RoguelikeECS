using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TheAlchemist.Components
{
    using static TheAlchemist.Systems.ItemSystem;

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
