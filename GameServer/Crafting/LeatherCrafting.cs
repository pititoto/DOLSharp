/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using DOL.Database;
using DOL.Language;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS
{
	/// <summary>
	/// The leather crafting skill
	/// </summary>
	public class LeatherCrafting : AbstractCraftingSkill
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public LeatherCrafting()
		{
			Icon = 0x07;
			Name = LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "Crafting.Name.Leathercrafting");
			eSkill = eCraftingSkill.LeatherCrafting;
		}

		/// <summary>
		/// Check if  the player own all needed tools
		/// </summary>
		/// <param name="player">the crafting player</param>
		/// <param name="craftItemData">the object in construction</param>
		/// <returns>true if the player hold all needed tools</returns>
		public override bool CheckTool(GamePlayer player, DBCraftedItem craftItemData)
		{
			byte flags = 0;
			foreach (InventoryItem item in player.Inventory.GetItemRange(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
			{
				if(item == null || item.Object_Type != 0) continue;

				if (item.Name == LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "Crafting.CheckTool.SmithsHammer"))
				{
					if((flags & 0x01) == 0) flags |= 0x01;
					if(flags >= 0x03) break;
				}
				else if (item.Name == LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "Crafting.CheckTool.SewingKit"))
				{
					if((flags & 0x02) == 0) flags |= 0x02;
					if(flags >= 0x03) break;
				}
			}

            // Luhz Crafting Update: 
            // Crafting no longer requires hand-held tools!
            /*
			if(flags < 0x03)
			{
				if((flags & 0x01) == 0)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "Crafting.CheckTool.NotHaveTools", craftItemData.ItemTemplate.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					player.Out.SendMessage(LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "Crafting.CheckTool.FindSmithTool"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return false;
				}

				if((flags & 0x02) == 0)
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "Crafting.CheckTool.NotHaveTools", craftItemData.ItemTemplate.Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					player.Out.SendMessage(LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "Crafting.CheckTool.FindSewingKit"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return false;
				}
			}
            */
			return true;
		}

		/// <summary>
		/// Select craft to gain point and increase it
		/// </summary>
		/// <param name="player"></param>
		/// <param name="item"></param>
		public override void GainCraftingSkillPoints(GamePlayer player, DBCraftedItem item)
		{
            // Luhz Crafting Update:
            // "Secondary" tradeskills are no longer limited by "Primary" tradeskills - Patch 1.87
			//if (player.GetCraftingSkillValue(eCraftingSkill.LeatherCrafting) < player.GetCraftingSkillValue(player.CraftingPrimarySkill)) // max secondary skill cap == primary skill
			//{
				if(Util.Chance( CalculateChanceToGainPoint(player, item)))
				{
					player.GainCraftingSkill(eCraftingSkill.LeatherCrafting, 1);
					player.Out.SendUpdateCraftingSkills();
				}
			//}
		}
	}
}
