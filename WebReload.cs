using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace WeaponPaints;

// skins website on the LAN: logs players in by the IP they play from
public partial class WeaponPaints
{
	private static string? PlayerIp(CCSPlayerController player)
	{
		var ip = player.IpAddress?.Split(':')[0];
		return string.IsNullOrEmpty(ip) || ip == "loopback" ? "127.0.0.1" : ip;
	}

	private static IEnumerable<CCSPlayerController> HumanPlayers() =>
		Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: false, IsHLTV: false });

	private static HashSet<string> OwnAddresses()
	{
		var set = new HashSet<string> { "127.0.0.1" };
		foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
		foreach (var a in nic.GetIPProperties().UnicastAddresses)
			if (a.Address.AddressFamily == AddressFamily.InterNetwork) set.Add(a.Address.ToString());
		return set;
	}

	private static bool IsPrivateLan(IPAddress ip)
	{
		var b = ip.GetAddressBytes();
		return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168);
	}

	// up adapter with a gateway and a private IPv4, skips VPN/virtual ones
	private static string? LanIp() =>
		NetworkInterface.GetAllNetworkInterfaces()
			.Where(n => n.OperationalStatus == OperationalStatus.Up &&
			            n.NetworkInterfaceType is not (NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) &&
			            n.GetIPProperties().GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork))
			.SelectMany(n => n.GetIPProperties().UnicastAddresses)
			.Select(a => a.Address)
			.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork && IsPrivateLan(a))?.ToString();

	// menu pictures from the site's copies while it answers, else from GitHub
	private const string UpstreamImages = "https://raw.githubusercontent.com/Nereziel/cs2-WeaponPaints/main/website/img/skins/";
	private static readonly HttpClient SiteHttp = new() { Timeout = TimeSpan.FromSeconds(3) };
	private volatile string? _menuImageBase;
	private DateTime _menuImageCheckedAt = DateTime.MinValue;

	private string? SiteImageBase()
	{
		if (string.Equals(Config.Website, "auto", StringComparison.OrdinalIgnoreCase))
			return $"http://{LanIp() ?? "127.0.0.1"}/img/";
		var site = Config.Website.Trim();
		if (site.Length == 0) return null;
		if (!site.Contains("://")) site = "http://" + site;
		return site.TrimEnd('/') + "/img/";
	}

	private void CheckSiteImages()
	{
		_menuImageCheckedAt = DateTime.UtcNow;
		var imageBase = SiteImageBase();
		if (imageBase == null)
		{
			_menuImageBase = null;
			return;
		}
		_ = Task.Run(async () =>
		{
			try
			{
				using var res = await SiteHttp.GetAsync(imageBase + "ok");
				_menuImageBase = res.IsSuccessStatusCode ? imageBase : null;
			}
			catch
			{
				_menuImageBase = null;
			}
		});
	}

	private string MenuImage(string image)
	{
		// the site can be started or stopped mid-map
		var age = DateTime.UtcNow - _menuImageCheckedAt;
		if (age > TimeSpan.FromSeconds(_menuImageBase == null ? 60 : 300)) CheckSiteImages();

		var imageBase = _menuImageBase;
		return imageBase != null && image.StartsWith(UpstreamImages, StringComparison.Ordinal)
			? imageBase + image[UpstreamImages.Length..]
			: image;
	}

	// "auto": computer name, or LAN IP for generated names like DESKTOP-7H3K2LF
	private string SiteAddress()
	{
		if (!string.Equals(Config.Website, "auto", StringComparison.OrdinalIgnoreCase)) return Config.Website;
		var name = Environment.MachineName.ToLowerInvariant();
		var generated = Regex.IsMatch(name, @"^(desktop|win|laptop)-[a-z0-9]{5,}$");
		return generated ? LanIp() ?? $"{name}/" : $"{name}/";
	}

	// "auto" is shown only while the site answers
	private string? ShownSiteAddress()
	{
		if (string.IsNullOrWhiteSpace(Config.Website)) return null;
		if (!string.Equals(Config.Website, "auto", StringComparison.OrdinalIgnoreCase)) return Config.Website;
		if (DateTime.UtcNow - _menuImageCheckedAt > TimeSpan.FromSeconds(60)) CheckSiteImages();
		return _menuImageBase != null ? SiteAddress() : null;
	}

	[ConsoleCommand("css_site", "Address of the skins website")]
	[CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
	public void OnCommandSite(CCSPlayerController? player, CommandInfo info)
	{
		if (player is not { IsValid: true, IsBot: false }) return;
		var address = ShownSiteAddress();
		player.Print(address != null ? Localizer["wp_site_link", address] : Localizer["wp_site_off"]);
	}

	// site login: the one player on this IP ("local" = this machine)
	[ConsoleCommand("css_wp_who", "Website login: player playing from an IP")]
	[CommandHelper(minArgs: 1, usage: "<ip|local>", whoCanExecute: CommandUsage.SERVER_ONLY)]
	public void OnCommandWebWho(CCSPlayerController? caller, CommandInfo info)
	{
		var ip = info.GetArg(1).Trim();
		var own = ip == "local" ? OwnAddresses() : null;
		var found = HumanPlayers().Where(p => PlayerIp(p) is { } pip && (own?.Contains(pip) ?? pip == ip)).ToList();
		var answer = found.Count switch
		{
			0 => "none",
			1 => $"{found[0].SteamID}",
			_ => "many",
		};
		info.ReplyToCommand($"[WeaponPaints] wp_who {ip}: {answer}");
	}

	// site session check
	[ConsoleCommand("css_wp_ip", "Website session: IP of an online player")]
	[CommandHelper(minArgs: 1, usage: "<steamid64>", whoCanExecute: CommandUsage.SERVER_ONLY)]
	public void OnCommandWebIp(CCSPlayerController? caller, CommandInfo info)
	{
		var steamId = info.GetArg(1).Trim();
		var player = HumanPlayers().FirstOrDefault(p => p.SteamID.ToString() == steamId);
		info.ReplyToCommand($"[WeaponPaints] wp_ip {steamId}: {(player == null ? "offline" : PlayerIp(player))}");
	}

	// the site calls this after a save so the change applies at once
	[ConsoleCommand("css_wp_reload", "Reload a player's skins from the database")]
	[CommandHelper(minArgs: 1, usage: "<steamid64>", whoCanExecute: CommandUsage.SERVER_ONLY)]
	public void OnCommandWebReload(CCSPlayerController? caller, CommandInfo info)
	{
		var steamId = info.GetArg(1).Trim();
		var player = Utilities.GetPlayers().FirstOrDefault(p => p is { IsValid: true, IsBot: false } && p.SteamID.ToString() == steamId);

		if (player == null || WeaponSync == null)
		{
			info.ReplyToCommand($"[WeaponPaints] wp_reload {steamId}: offline");
			return;
		}

		var slot = player.Slot;
		var playerInfo = new PlayerInfo
		{
			UserId = player.UserId,
			Slot = slot,
			Index = (int)player.Index,
			SteamId = steamId,
			Name = player.PlayerName,
			IpAddress = player.IpAddress?.Split(":")[0]
		};

		// loading only adds rows: clear first or removed skins stay
		GPlayerWeaponsInfo.TryRemove(slot, out _);
		GPlayersKnife.TryRemove(slot, out _);
		GPlayersGlove.TryRemove(slot, out _);
		GPlayersAgent.TryRemove(slot, out _);
		GPlayersMusic.TryRemove(slot, out _);
		GPlayersPin.TryRemove(slot, out _);

		info.ReplyToCommand($"[WeaponPaints] wp_reload {steamId}: ok");

		var sync = WeaponSync;
		_ = Task.Run(async () =>
		{
			await sync.GetPlayerData(playerInfo);
			Server.NextFrame(() =>
			{
				if (!player.IsValid || !player.PawnIsAlive) return;
				GivePlayerGloves(player);
				RefreshWeapons(player);
				GivePlayerAgent(player);
				GivePlayerMusicKit(player);
				AddTimer(0.15f, () => GivePlayerPin(player));
			});
		});
	}
}
