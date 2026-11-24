using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using System.Text;

namespace WeaponPaints;

public static class PlayerExtensions
{
	public static void Print(this CCSPlayerController controller, string message)
	{
		if (WeaponPaints._localizer == null) return;

		var prefix = WeaponPaints.Instance.Config.ChatPrefix;
		StringBuilder _message = new(string.IsNullOrWhiteSpace(prefix)
			? WeaponPaints._localizer["wp_prefix"]
			: prefix.TrimEnd().ReplaceColorTags() + " ");
		_message.Append(message);
		controller.PrintToChat(_message.ToString());
	}
}
