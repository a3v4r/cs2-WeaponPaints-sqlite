using Dapper;
using System.Data;
using System.Collections.Concurrent;
using CounterStrikeSharp.API.Modules.Utils;
using System.Globalization;

namespace WeaponPaints;

internal class WeaponSynchronization
{
	private readonly WeaponPaintsConfig _config;
	private readonly Database _database;

	internal WeaponSynchronization(Database database, WeaponPaintsConfig config)
	{
		_database = database;
		_config = config;
	}

	internal async Task GetPlayerData(PlayerInfo? player)
	{
		try
		{
			using var connection = await _database.GetConnectionAsync();

			if (_config.Additional.KnifeEnabled)
				GetKnifeFromDatabase(player, connection);
			if (_config.Additional.GloveEnabled)
				GetGloveFromDatabase(player, connection);
			if (_config.Additional.AgentEnabled)
				GetAgentFromDatabase(player, connection);
			if (_config.Additional.MusicEnabled)
				GetMusicFromDatabase(player, connection);
			if (_config.Additional.SkinEnabled)
				GetWeaponPaintsFromDatabase(player, connection);
			if (_config.Additional.PinsEnabled)
				GetPinsFromDatabase(player, connection);
		}
		catch (Exception ex)
		{
			Utility.Log($"Database connection error in GetPlayerData: {ex.Message}");
		}
	}

	private void GetKnifeFromDatabase(PlayerInfo? player, IDbConnection connection)
	{
		try
		{
			if (!_config.Additional.KnifeEnabled || string.IsNullOrEmpty(player?.SteamId))
				return;

			const string query = "SELECT `knife`, `weapon_team` FROM `wp_player_knife` WHERE `steamid` = @steamid ORDER BY `weapon_team` ASC";
			var rows = connection.Query<dynamic>(query, new { steamid = player.SteamId });

			foreach (var row in rows)
			{
				if (string.IsNullOrEmpty(row.knife)) continue;

				CsTeam weaponTeam = Convert.ToInt32(row.weapon_team) switch
				{
					2 => CsTeam.Terrorist,
					3 => CsTeam.CounterTerrorist,
					_ => CsTeam.None,
				};

				var playerKnives = WeaponPaints.GPlayersKnife.GetOrAdd(player.Slot, _ => new ConcurrentDictionary<CsTeam, string>());

				if (weaponTeam == CsTeam.None)
				{
					playerKnives[CsTeam.Terrorist] = row.knife.ToString();
					playerKnives[CsTeam.CounterTerrorist] = row.knife.ToString();
				}
				else
				{
					playerKnives[weaponTeam] = row.knife.ToString();
				}
			}
		}
		catch (Exception ex)
		{
			Utility.Log($"An error occurred in GetKnifeFromDatabase: {ex.Message}");
		}
	}

	private void GetGloveFromDatabase(PlayerInfo? player, IDbConnection connection)
	{
		try
		{
			if (!_config.Additional.GloveEnabled || string.IsNullOrEmpty(player?.SteamId))
				return;

			const string query = "SELECT `weapon_defindex`, `weapon_team` FROM `wp_player_gloves` WHERE `steamid` = @steamid ORDER BY `weapon_team` ASC";
			var rows = connection.Query<dynamic>(query, new { steamid = player.SteamId });

			foreach (var row in rows)
			{
				if (row.weapon_defindex == null) continue;
				var playerGloves = WeaponPaints.GPlayersGlove.GetOrAdd(player.Slot, _ => new ConcurrentDictionary<CsTeam, ushort>());
				CsTeam weaponTeam = Convert.ToInt32(row.weapon_team) switch
				{
					2 => CsTeam.Terrorist,
					3 => CsTeam.CounterTerrorist,
					_ => CsTeam.None,
				};

				if (weaponTeam == CsTeam.None)
				{
					playerGloves[CsTeam.Terrorist] = Convert.ToUInt16(row.weapon_defindex);
					playerGloves[CsTeam.CounterTerrorist] = Convert.ToUInt16(row.weapon_defindex);
				}
				else
				{
					playerGloves[weaponTeam] = Convert.ToUInt16(row.weapon_defindex);
				}
			}
		}
		catch (Exception ex)
		{
			Utility.Log($"An error occurred in GetGlovesFromDatabase: {ex.Message}");
		}
	}

	private void GetAgentFromDatabase(PlayerInfo? player, IDbConnection connection)
	{
		try
		{
			if (!_config.Additional.AgentEnabled || string.IsNullOrEmpty(player?.SteamId))
				return;

			const string query = "SELECT `agent_ct`, `agent_t` FROM `wp_player_agents` WHERE `steamid` = @steamid";
			var agentData = connection.QueryFirstOrDefault<dynamic>(query, new { steamid = player.SteamId });

			if (agentData == null) return;
			string agentCT = agentData.agent_ct?.ToString() ?? "";
			string agentT = agentData.agent_t?.ToString() ?? "";

			if (!string.IsNullOrEmpty(agentCT) || !string.IsNullOrEmpty(agentT))
			{
				WeaponPaints.GPlayersAgent[player.Slot] = (agentCT, agentT);
			}
		}
		catch (Exception ex)
		{
			Utility.Log($"An error occurred in GetAgentFromDatabase: {ex.Message}");
		}
	}

	private void GetWeaponPaintsFromDatabase(PlayerInfo? player, IDbConnection connection)
	{
		try
		{
			if (!_config.Additional.SkinEnabled || player == null || string.IsNullOrEmpty(player.SteamId))
				return;
				
			var playerWeapons = WeaponPaints.GPlayerWeaponsInfo.GetOrAdd(player.Slot,
				_ => new ConcurrentDictionary<CsTeam, ConcurrentDictionary<int, WeaponInfo>>());

			const string query = "SELECT * FROM `wp_player_skins` WHERE `steamid` = @steamid ORDER BY `weapon_team` ASC";
			var playerSkins = connection.Query<dynamic>(query, new { steamid = player.SteamId });

			foreach (var row in playerSkins)
			{
				int weaponDefIndex = Convert.ToInt32(row.weapon_defindex ?? 0);
				int weaponPaintId = Convert.ToInt32(row.weapon_paint_id ?? 0);
				float weaponWear = Convert.ToSingle(row.weapon_wear ?? 0f);
				int weaponSeed = Convert.ToInt32(row.weapon_seed ?? 0);
				string weaponNameTag = row.weapon_nametag?.ToString() ?? "";
				bool weaponStatTrak = Convert.ToInt32(row.weapon_stattrak ?? 0) != 0;
				int weaponStatTrakCount = Convert.ToInt32(row.weapon_stattrak_count ?? 0);
				
				CsTeam weaponTeam = Convert.ToInt32(row.weapon_team ?? 0) switch
				{
					2 => CsTeam.Terrorist,
					3 => CsTeam.CounterTerrorist,
					_ => CsTeam.None,
				};
						
				string[]? keyChainParts = row.weapon_keychain?.ToString().Split(';');
				KeyChainInfo keyChainInfo = new KeyChainInfo();

				if (keyChainParts != null && keyChainParts.Length == 5 &&
				    uint.TryParse(keyChainParts[0], out uint keyChainId) &&
				    float.TryParse(keyChainParts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float keyChainOffsetX) &&
				    float.TryParse(keyChainParts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float keyChainOffsetY) &&
				    float.TryParse(keyChainParts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float keyChainOffsetZ) &&
				    uint.TryParse(keyChainParts[4], out uint keyChainSeed))
				{
					keyChainInfo.Id = keyChainId;
					keyChainInfo.OffsetX = keyChainOffsetX;
					keyChainInfo.OffsetY = keyChainOffsetY;
					keyChainInfo.OffsetZ = keyChainOffsetZ;
					keyChainInfo.Seed = keyChainSeed;
				}

				WeaponInfo weaponInfo = new WeaponInfo
				{
					Paint = weaponPaintId,
					Seed = weaponSeed,
					Wear = weaponWear,
					Nametag = weaponNameTag,
					KeyChain = keyChainInfo,
					StatTrak = weaponStatTrak,
					StatTrakCount = weaponStatTrakCount,
				};

				for (int i = 0; i <= 4; i++)
				{
					string stickerColumn = $"weapon_sticker_{i}";
					var stickerData = ((IDictionary<string, object>)row!)[stickerColumn];

					if (stickerData == null || string.IsNullOrEmpty(stickerData.ToString())) continue;
						
					var parts = stickerData.ToString()!.Split(';');

					if (parts.Length != 7 ||
					    !uint.TryParse(parts[0], out uint stickerId) ||
					    !uint.TryParse(parts[1], out uint stickerSchema) ||
					    !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float stickerOffsetX) ||
					    !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float stickerOffsetY) ||
					    !float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float stickerWear) ||
					    !float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float stickerScale) ||
					    !float.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out float stickerRotation)) continue;
						
					StickerInfo stickerInfo = new StickerInfo
					{
						Id = stickerId,
						Schema = stickerSchema,
						OffsetX = stickerOffsetX,
						OffsetY = stickerOffsetY,
						Wear = stickerWear,
						Scale = stickerScale,
						Rotation = stickerRotation
					};

					weaponInfo.Stickers.Add(stickerInfo);
				}
					
				if (weaponTeam == CsTeam.None)
				{
					var terroristWeapons = playerWeapons.GetOrAdd(CsTeam.Terrorist, _ => new ConcurrentDictionary<int, WeaponInfo>());
					var counterTerroristWeapons = playerWeapons.GetOrAdd(CsTeam.CounterTerrorist, _ => new ConcurrentDictionary<int, WeaponInfo>());
					terroristWeapons[weaponDefIndex] = weaponInfo;
					counterTerroristWeapons[weaponDefIndex] = weaponInfo;
				}
				else
				{
					var teamWeapons = playerWeapons.GetOrAdd(weaponTeam, _ => new ConcurrentDictionary<int, WeaponInfo>());
					teamWeapons[weaponDefIndex] = weaponInfo;
				}
			}
		}
		catch (Exception ex)
		{
			Utility.Log($"An error occurred in GetWeaponPaintsFromDatabase: {ex.Message}");
		}
	}

	private void GetMusicFromDatabase(PlayerInfo? player, IDbConnection connection)
	{
		try
		{
			if (!_config.Additional.MusicEnabled || string.IsNullOrEmpty(player?.SteamId))
				return;

			const string query = "SELECT `music_id`, `weapon_team` FROM `wp_player_music` WHERE `steamid` = @steamid ORDER BY `weapon_team` ASC";
			var rows = connection.Query<dynamic>(query, new { steamid = player.SteamId });

			foreach (var row in rows)
			{
				if (row.music_id == null) continue;

				CsTeam weaponTeam = Convert.ToInt32(row.weapon_team) switch
				{
					2 => CsTeam.Terrorist,
					3 => CsTeam.CounterTerrorist,
					_ => CsTeam.None,
				};

				var playerMusic = WeaponPaints.GPlayersMusic.GetOrAdd(player.Slot, _ => new ConcurrentDictionary<CsTeam, ushort>());

				if (weaponTeam == CsTeam.None)
				{
					playerMusic[CsTeam.Terrorist] = Convert.ToUInt16(row.music_id);
					playerMusic[CsTeam.CounterTerrorist] = Convert.ToUInt16(row.music_id);
				}
				else
				{
					playerMusic[weaponTeam] = Convert.ToUInt16(row.music_id);
				}
			}
		}
		catch (Exception ex)
		{
			Utility.Log($"An error occurred in GetMusicFromDatabase: {ex.Message}");
		}
	}

	private void GetPinsFromDatabase(PlayerInfo? player, IDbConnection connection)
	{
		try
		{
			if (string.IsNullOrEmpty(player?.SteamId))
				return;

			const string query = "SELECT `id`, `weapon_team` FROM `wp_player_pins` WHERE `steamid` = @steamid ORDER BY `weapon_team` ASC";
			var rows = connection.Query<dynamic>(query, new { steamid = player.SteamId });

			foreach (var row in rows)
			{
				if (row.id == null) continue;

				CsTeam weaponTeam = Convert.ToInt32(row.weapon_team) switch
				{
					2 => CsTeam.Terrorist,
					3 => CsTeam.CounterTerrorist,
					_ => CsTeam.None,
				};

				var playerPins = WeaponPaints.GPlayersPin.GetOrAdd(player.Slot, _ => new ConcurrentDictionary<CsTeam, ushort>());

				if (weaponTeam == CsTeam.None)
				{
					playerPins[CsTeam.Terrorist] = Convert.ToUInt16(row.id);
					playerPins[CsTeam.CounterTerrorist] = Convert.ToUInt16(row.id);
				}
				else
				{
					playerPins[weaponTeam] = Convert.ToUInt16(row.id);
				}
			}
		}
		catch (Exception ex)
		{
			Utility.Log($"An error occurred in GetPinsFromDatabase: {ex.Message}");
		}
	}

	internal async Task SyncKnifeToDatabase(PlayerInfo player, string knife, CsTeam[] teams)
	{
		if (!_config.Additional.KnifeEnabled || string.IsNullOrEmpty(player.SteamId) || string.IsNullOrEmpty(knife) || teams.Length == 0) return;

		string query = _database.GetDbType() == "sqlite" 
			? "INSERT INTO `wp_player_knife` (`steamid`, `weapon_team`, `knife`) VALUES(@steamid, @team, @newKnife) ON CONFLICT(`steamid`, `weapon_team`) DO UPDATE SET `knife` = @newKnife"
			: "INSERT INTO `wp_player_knife` (`steamid`, `weapon_team`, `knife`) VALUES(@steamid, @team, @newKnife) ON DUPLICATE KEY UPDATE `knife` = @newKnife";

		try
		{
			using var connection = await _database.GetConnectionAsync();
			foreach (var team in teams)
			{
				await connection.ExecuteAsync(query, new { steamid = player.SteamId, team = (int)team, newKnife = knife });
			}
		}
		catch (Exception e)
		{
			Utility.Log($"Error syncing knife to database: {e.Message}");
		}
	}
	
	internal async Task SyncGloveToDatabase(PlayerInfo player, ushort gloveDefIndex, CsTeam[] teams)
	{
		if (!_config.Additional.GloveEnabled || string.IsNullOrEmpty(player.SteamId) || teams.Length == 0) 
			return;

		string query = _database.GetDbType() == "sqlite"
			? "INSERT INTO `wp_player_gloves` (`steamid`, `weapon_team`, `weapon_defindex`) VALUES(@steamid, @team, @gloveDefIndex) ON CONFLICT(`steamid`, `weapon_team`) DO UPDATE SET `weapon_defindex` = @gloveDefIndex"
			: "INSERT INTO `wp_player_gloves` (`steamid`, `weapon_team`, `weapon_defindex`) VALUES(@steamid, @team, @gloveDefIndex) ON DUPLICATE KEY UPDATE `weapon_defindex` = @gloveDefIndex";

		try
		{
			using var connection = await _database.GetConnectionAsync();
			foreach (var team in teams)
			{
				await connection.ExecuteAsync(query, new { 
					steamid = player.SteamId, 
					team = (int)team,
					gloveDefIndex 
				});
			}
		}
		catch (Exception e)
		{
			Utility.Log($"Error syncing glove to database: {e.Message}");
		}
	}

	internal async Task SyncAgentToDatabase(PlayerInfo player)
	{
		if (!_config.Additional.AgentEnabled || string.IsNullOrEmpty(player.SteamId)) return;

		string query = _database.GetDbType() == "sqlite"
			? "INSERT INTO `wp_player_agents` (`steamid`, `agent_ct`, `agent_t`) VALUES(@steamid, @agent_ct, @agent_t) ON CONFLICT(`steamid`) DO UPDATE SET `agent_ct` = @agent_ct, `agent_t` = @agent_t"
			: "INSERT INTO `wp_player_agents` (`steamid`, `agent_ct`, `agent_t`) VALUES(@steamid, @agent_ct, @agent_t) ON DUPLICATE KEY UPDATE `agent_ct` = @agent_ct, `agent_t` = @agent_t";

		try
		{
			using var connection = await _database.GetConnectionAsync();
			await connection.ExecuteAsync(query, new { steamid = player.SteamId, agent_ct = WeaponPaints.GPlayersAgent[player.Slot].CT, agent_t = WeaponPaints.GPlayersAgent[player.Slot].T });
		}
		catch (Exception e)
		{
			Utility.Log($"Error syncing agents to database: {e.Message}");
		}
	}

	internal async Task SyncWeaponPaintsToDatabase(PlayerInfo player)
	{
		if (string.IsNullOrEmpty(player.SteamId) || !WeaponPaints.GPlayerWeaponsInfo.TryGetValue(player.Slot, out var teamWeaponInfos))
			return;

		try
		{
			using var connection = await _database.GetConnectionAsync();

			foreach (var (teamId, weaponsInfo) in teamWeaponInfos)
			{
				foreach (var (weaponDefIndex, weaponInfo) in weaponsInfo)
				{
					var paintId = weaponInfo.Paint;
					var wear = weaponInfo.Wear;
					var seed = weaponInfo.Seed;

					const string queryCheckExistence = "SELECT COUNT(*) FROM `wp_player_skins` WHERE `steamid` = @steamid AND `weapon_defindex` = @weaponDefIndex AND `weapon_team` = @weaponTeam";
		                
					var existingRecordCount = await connection.ExecuteScalarAsync<int>(
						queryCheckExistence, 
						new { steamid = player.SteamId, weaponDefIndex, weaponTeam = (int)teamId }
					);

					string query;
					object parameters;

					if (existingRecordCount > 0)
					{
						query = "UPDATE `wp_player_skins` SET `weapon_paint_id` = @paintId, `weapon_wear` = @wear, `weapon_seed` = @seed " +
						        "WHERE `steamid` = @steamid AND `weapon_defindex` = @weaponDefIndex AND `weapon_team` = @weaponTeam";
						parameters = new { steamid = player.SteamId, weaponDefIndex, weaponTeam = (int)teamId, paintId, wear, seed };
					}
					else
					{
						query = "INSERT INTO `wp_player_skins` (`steamid`, `weapon_defindex`, `weapon_team`, `weapon_paint_id`, `weapon_wear`, `weapon_seed`) " +
						        "VALUES (@steamid, @weaponDefIndex, @weaponTeam, @paintId, @wear, @seed)";
						parameters = new { steamid = player.SteamId, weaponDefIndex, weaponTeam = (int)teamId, paintId, wear, seed };
					}

					await connection.ExecuteAsync(query, parameters);
				}
			}
		}
		catch (Exception e)
		{
			Utility.Log($"Error syncing weapon paints to database: {e.Message}");
		}
	}

	internal async Task SyncMusicToDatabase(PlayerInfo player, ushort music, CsTeam[] teams)
	{
		if (!_config.Additional.MusicEnabled || string.IsNullOrEmpty(player.SteamId)) return;

		string query = _database.GetDbType() == "sqlite"
			? "INSERT INTO `wp_player_music` (`steamid`, `weapon_team`, `music_id`) VALUES(@steamid, @team, @newMusic) ON CONFLICT(`steamid`, `weapon_team`) DO UPDATE SET `music_id` = @newMusic"
			: "INSERT INTO `wp_player_music` (`steamid`, `weapon_team`, `music_id`) VALUES(@steamid, @team, @newMusic) ON DUPLICATE KEY UPDATE `music_id` = @newMusic";

		try
		{
			using var connection = await _database.GetConnectionAsync();
			foreach (var team in teams)
			{
				await connection.ExecuteAsync(query, new { steamid = player.SteamId, team = (int)team, newMusic = music });
			}
		}
		catch (Exception e)
		{
			Utility.Log($"Error syncing music kit to database: {e.Message}");
		}
	}
		
	internal async Task SyncPinToDatabase(PlayerInfo player, ushort pin, CsTeam[] teams)
	{
		if (!_config.Additional.PinsEnabled || string.IsNullOrEmpty(player.SteamId)) return;

		string query = _database.GetDbType() == "sqlite"
			? "INSERT INTO `wp_player_pins` (`steamid`, `weapon_team`, `id`) VALUES(@steamid, @team, @newPin) ON CONFLICT(`steamid`, `weapon_team`) DO UPDATE SET `id` = @newPin"
			: "INSERT INTO `wp_player_pins` (`steamid`, `weapon_team`, `id`) VALUES(@steamid, @team, @newPin) ON DUPLICATE KEY UPDATE `id` = @newPin";

		try
		{
			using var connection = await _database.GetConnectionAsync();
			foreach (var team in teams)
			{
				await connection.ExecuteAsync(query, new { steamid = player.SteamId, team = (int)team, newPin = pin });
			}
		}
		catch (Exception e)
		{
			Utility.Log($"Error syncing pin to database: {e.Message}");
		}
	}

	internal async Task SyncStatTrakToDatabase(PlayerInfo player)
	{
	    if (WeaponPaints.WeaponSync == null || WeaponPaints.GPlayerWeaponsInfo.IsEmpty) return;
	    if (string.IsNullOrEmpty(player.SteamId))
	        return;

	    try
	    {
	        using var connection = await _database.GetConnectionAsync();
			using var transaction = connection.BeginTransaction();

	        if (!WeaponPaints.GPlayerWeaponsInfo.TryGetValue(player.Slot, out var teamWeaponsInfo))
	            return;
	        
	        foreach (var teamInfo in teamWeaponsInfo)
	        {
	            var weaponInfos = teamInfo.Value;
	            var statTrakWeapons = weaponInfos
		            .ToDictionary(
			            w => w.Key, 
			            w => (w.Value.StatTrak, w.Value.StatTrakCount)
		            );

	            if (statTrakWeapons.Count == 0) continue;
	            int weaponTeam = (int)teamInfo.Key;

	            foreach (var (defindex, (statTrak, statTrakCount)) in statTrakWeapons)
	            {
		            const string query = @"
					    UPDATE `wp_player_skins` 
					    SET `weapon_stattrak` = @StatTrak, 
					        `weapon_stattrak_count` = @StatTrakCount
					    WHERE `steamid` = @steamid 
					      AND `weapon_defindex` = @weaponDefIndex
					      AND `weapon_team` = @weaponTeam";

	                var parameters = new
	                {
	                    steamid = player.SteamId,
	                    weaponDefIndex = defindex,
	                    StatTrak = statTrak,
	                    StatTrakCount = statTrakCount,
	                    weaponTeam
	                };

	                await connection.ExecuteAsync(query, parameters, transaction);
	            }
	        }

	        transaction.Commit();
	    }
	    catch (Exception e)
	    {
	        Utility.Log($"Error syncing stattrak to database: {e.Message}");
	    }
	}
}