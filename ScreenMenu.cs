using System.Text;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;

namespace WeaponPaints;

// center HTML menu, no MenuManager: A/D scroll, E pick, R close
internal class ScreenMenu(string title) : IMenu
{
	public string Title { get; set; } = title;
	public List<ChatMenuOption> MenuOptions { get; } = [];
	public PostSelectAction PostSelectAction { get; set; } = PostSelectAction.Close;
	public bool ExitButton { get; set; } = true;

	// handlers still get option.Text
	internal readonly Dictionary<ChatMenuOption, (string Display, string Image)> Meta = new();

	internal Func<CCSPlayerController, string?>? Current;

	public ChatMenuOption AddMenuOption(string display, Action<CCSPlayerController, ChatMenuOption> onSelect, bool disabled = false)
	{
		var option = new ChatMenuOption(display, disabled, onSelect);
		MenuOptions.Add(option);
		return option;
	}

	public void Open(CCSPlayerController player) => WeaponPaints.Instance.OpenScreenMenu(player, this);

	public void OpenToAll()
	{
		foreach (var player in Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: false }))
			Open(player);
	}
}

public partial class WeaponPaints
{
	private sealed class OpenedMenu
	{
		public required ScreenMenu Menu;
		public int Selected;
		public PlayerButtons Prev;
	}

	private readonly Dictionary<int, OpenedMenu> _screenMenus = new();

	internal void OpenScreenMenu(CCSPlayerController player, ScreenMenu menu)
	{
		if (!player.IsValid || menu.MenuOptions.Count == 0) return;
		if (!player.PawnIsAlive)
		{
			player.Print(Localizer["wp_menu_alive"]);
			return;
		}

		var current = menu.Current?.Invoke(player);
		var selected = current == null ? -1 : menu.MenuOptions.FindIndex(o => o.Text == current);

		_screenMenus[player.Slot] = new OpenedMenu { Menu = menu, Selected = Math.Max(selected, 0), Prev = player.Buttons };
		player.Print(Localizer["wp_menu_hint"]);
	}

	private void CloseScreenMenu(int slot)
	{
		if (!_screenMenus.Remove(slot)) return;
		var player = Utilities.GetPlayerFromSlot(slot);
		if (player is { IsValid: true }) player.PrintToCenterHtml(" ");
	}

	private void OnScreenMenuTick()
	{
		if (_screenMenus.Count == 0) return;

		foreach (var slot in _screenMenus.Keys.ToList())
		{
			var player = Utilities.GetPlayerFromSlot(slot);
			if (player is not { IsValid: true } || !player.PawnIsAlive)
			{
				CloseScreenMenu(slot);
				continue;
			}

			var open = _screenMenus[slot];
			var options = open.Menu.MenuOptions;
			var buttons = player.Buttons;
			var prev = open.Prev;
			open.Prev = buttons;
			bool Hit(PlayerButtons b) => (buttons & b) != 0 && (prev & b) == 0;

			if (Hit(PlayerButtons.Moveleft)) open.Selected = (open.Selected - 1 + options.Count) % options.Count;
			else if (Hit(PlayerButtons.Moveright)) open.Selected = (open.Selected + 1) % options.Count;
			else if (Hit(PlayerButtons.Reload)) { CloseScreenMenu(slot); continue; }
			else if (Hit(PlayerButtons.Use))
			{
				var option = options[open.Selected];
				if (option.Disabled) continue;
				// close first: the handler may open another menu
				if (open.Menu.PostSelectAction == PostSelectAction.Close) CloseScreenMenu(slot);
				option.OnSelect?.Invoke(player, option);
				continue;
			}

			// A/D scroll the menu
			var pawn = player.PlayerPawn.Value;
			if (pawn != null)
			{
				pawn.AbsVelocity.X = 0;
				pawn.AbsVelocity.Y = 0;
			}

			RenderScreenMenu(player, open);
		}
	}

	private void RenderScreenMenu(CCSPlayerController player, OpenedMenu open)
	{
		var option = open.Menu.MenuOptions[open.Selected];
		var (display, image) = open.Menu.Meta.TryGetValue(option, out var meta) ? meta : (option.Text, "");

		var sb = new StringBuilder();
		sb.Append($"<font color='#ffcc33' class='fontSize-l'>{Html(display)}</font>&nbsp; <font color='#7f8ca3' class='fontSize-sm'>({open.Selected + 1}/{open.Menu.MenuOptions.Count})</font><br>");
		sb.Append("<font color='#cfd8e6' class='fontSize-sm'>");
		sb.Append($"<font color='#ffcc33'>A</font>/<font color='#ffcc33'>D</font>&nbsp;{Html(Localizer["wp_menu_scroll"])}&nbsp;&nbsp;");
		sb.Append($"<font color='#ffcc33'>E</font>&nbsp;{Html(Localizer["wp_menu_select"])}&nbsp;&nbsp;");
		sb.Append($"<font color='#ffcc33'>R</font>&nbsp;{Html(Localizer["wp_menu_exit"])}</font>");
		if (Config.Additional.ShowSkinImage && !string.IsNullOrEmpty(image))
			sb.Append($"<br><img src='{MenuImage(image)}'>");

		player.PrintToCenterHtml(sb.ToString());
	}

	// chat colours and {tags} don't work in center HTML
	private static string Html(string text)
	{
		var clean = Regex.Replace(text, @"\{[a-zA-Z]+\}|[\x00-\x1F]", "").Trim();
		return clean.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
	}
}
