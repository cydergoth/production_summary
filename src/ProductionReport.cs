using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProductionSummary
{
    /// <summary>
    /// Collects the production state of every base in the current sector and formats it as
    /// Unity rich text.
    /// </summary>
    internal static class ProductionReport
    {
        internal class StationReport
        {
            public Station Station;
            public string Text;
        }

        public static List<StationReport> Build(Station dockedStation)
        {
            var reports = new List<StationReport>();
            TSector sector = GameData.data?.GetCurrentSector();
            if (sector == null)
            {
                return reports;
            }

            foreach (Station station in SectorStations(sector)
                .OrderByDescending(s => s == dockedStation)
                .ThenByDescending(s => s.PlayerOwned)
                .ThenBy(s => s.stationName(withLevel: false)))
            {
                string text;
                try
                {
                    text = DescribeStation(station, dockedStation);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning($"Could not read production of station {station.id}: {e}");
                    continue;
                }
                if (text != null)
                {
                    reports.Add(new StationReport { Station = station, Text = text });
                }
            }
            return reports;
        }

        private static IEnumerable<Station> SectorStations(TSector sector)
        {
            var seen = new HashSet<int>();
            var candidates = new List<Station>();
            if (sector.stationIDs != null)
            {
                foreach (int id in sector.stationIDs)
                {
                    candidates.Add(GameData.GetStation(id, allowDestroyedOrWrecked: false));
                }
            }
            if (sector.smallBases != null)
            {
                candidates.AddRange(sector.smallBases);
            }

            foreach (Station station in candidates)
            {
                if (station == null || station.DestroyedOrWrecked || !seen.Add(station.id))
                {
                    continue;
                }
                if (!station.discovered && !station.PlayerOwned && !Plugin.IncludeUndiscovered.Value)
                {
                    continue;
                }
                yield return station;
            }
        }

        /// <summary>Returns null when the station has nothing to report.</summary>
        private static string DescribeStation(Station station, Station dockedStation)
        {
            var fabricators = station.modules
                .OfType<SM_Fabricator>()
                .Where(f => f.producedItemID > 0 || Plugin.IncludeIdleModules.Value)
                .ToList();
            if (fabricators.Count == 0)
            {
                return null;
            }

            var sb = new StringBuilder();
            sb.Append("<size=16><b>").Append(StationColor(station))
              .Append(station.stationName(withLevel: true)).Append("</color></b></size>");
            string faction = FactionName(station);
            if (faction != null)
            {
                sb.Append("  ").Append(ColorSys.UITer).Append(faction).Append("</color>");
            }
            if (station == dockedStation)
            {
                sb.Append("  ").Append(ColorSys.cyan).Append(Loc.Get(Loc.DockedHere)).Append("</color>");
            }
            if (!station.InActivity)
            {
                sb.Append("  ").Append(ColorSys.infoNeg).Append(Loc.Get(Loc.Inactive)).Append("</color>");
            }
            if (station.HasCargoLink && station.stockLinkedStation != null)
            {
                sb.Append("\n").Append(ColorSys.UITer)
                  .Append(Loc.Get(Loc.UsesStorageOf, station.stockLinkedStation.stationName(withLevel: false)))
                  .Append("</color>");
            }

            foreach (SM_Fabricator fab in fabricators)
            {
                sb.Append("\n");
                DescribeModule(sb, station, fab);
            }
            return sb.ToString();
        }

        private static void DescribeModule(StringBuilder sb, Station station, SM_Fabricator fab)
        {
            Item product = fab.item;
            sb.Append("\n<b>").Append(fab.Name).Append("</b>: ");
            if (product == null)
            {
                sb.Append(ColorSys.UITer).Append(Loc.Get(Loc.NothingSelected)).Append("</color>");
                return;
            }

            string productName = "<b>" + ItemDB.GetItemNameModified(product, 0) + "</b>";
            if (fab is SM_Refinery)
            {
                sb.Append(Loc.Get(Loc.Refining, productName));
            }
            else
            {
                if (fab.ProductionYield > 1)
                {
                    sb.Append("<b>").Append(fab.ProductionYield).Append("x</b> ");
                }
                sb.Append(productName);
            }
            sb.Append("   ").Append(StatusString(fab));
            sb.Append("   ").Append(ColorSys.UITer).Append(Loc.Get(Loc.Cycle, FormatTime(fab.ProductionTime)))
              .Append("</color>");

            if (!(fab is SM_Refinery))
            {
                sb.Append("   ").Append(ColorSys.UITer).Append(Loc.Get(Loc.InStock)).Append(" </color>")
                  .Append(station.GetItemStationStock(product));
                if (fab.HasProductionLimit && fab.productionLimit > 0)
                {
                    sb.Append(ColorSys.UITer).Append(" ").Append(Loc.Get(Loc.Limit, fab.productionLimit)).Append("</color>");
                }
            }

            if (fab is SM_Mining mining)
            {
                sb.Append("\n    ").Append(ColorSys.UITer).Append(Loc.Get(Loc.ResourcesLeft)).Append(" </color>")
                  .Append(mining.ResourcesLeft);
                return;
            }

            List<ItemResource> materials = MaterialsOf(fab);
            if (materials == null || materials.Count == 0)
            {
                sb.Append("\n    ").Append(ColorSys.UITer).Append(Loc.Get(Loc.NoMaterials)).Append("</color>");
                return;
            }

            sb.Append("\n    ").Append(ColorSys.UITer).Append(Loc.Get(Loc.NeedsPerCycle)).Append("</color>");
            foreach (ItemResource material in materials)
            {
                sb.Append("\n      ").Append(MaterialLine(station, material));
            }
        }

        /// <summary>Mirrors the material requirements each module type checks in HasMaterials().</summary>
        private static List<ItemResource> MaterialsOf(SM_Fabricator fab)
        {
            if (fab is SM_Refinery)
            {
                return new List<ItemResource> { new ItemResource(fab.producedItemID, 1) };
            }
            return fab.Materials;
        }

        private static string MaterialLine(Station station, ItemResource material)
        {
            Item item = material.AsItem;
            string name = item != null ? ItemDB.GetItemNameModified(item, 0) : Loc.Get(Loc.UnknownItem, material.itemID);

            // GetItemStock() follows cargo links, so this is the stock the base itself consumes from.
            int stock = item != null ? station.GetItemStock().GetStockCount(item, null) : 0;
            int stash = 0;
            if (item != null && item.canBeStashed && station.PlayerOwned)
            {
                stash = station.StashedMaterial(new ItemResource(item.id, 1));
            }
            int available = stock + stash;
            string color = available >= material.quantity ? ColorSys.infoPos : ColorSys.infoNeg;

            var sb = new StringBuilder();
            sb.Append("<b>").Append(material.quantity).Append("x</b> ").Append(name)
              .Append("  ").Append(ColorSys.UITer).Append(Loc.Get(Loc.Supply)).Append(" </color>")
              .Append(color).Append("<b>").Append(stock).Append("</b></color>");
            if (stash > 0)
            {
                sb.Append(ColorSys.UITer).Append(" ").Append(Loc.Get(Loc.Stashed, stash)).Append("</color>");
            }
            if (material.quantity > 0)
            {
                sb.Append("  ").Append(ColorSys.UITer).Append(Loc.Get(Loc.Cycles, available / material.quantity))
                  .Append("</color>");
            }
            return sb.ToString();
        }

        private static string StatusString(SM_Fabricator fab)
        {
            if (!fab.IsPowered)
            {
                return ColorSys.infoNeg + "[" + Loc.Get(Loc.Unpowered) + "]</color>";
            }
            if (fab.IsProducing)
            {
                return ColorSys.infoPos + "[" + Loc.Get(Loc.Producing, fab.ProductionProgressPercentString) + "]</color>";
            }
            if (fab.ProductionLimitReached)
            {
                return ColorSys.infoNeg2 + "[" + Loc.Get(Loc.LimitReached) + "]</color>";
            }
            return ColorSys.infoNeg + "[" + Loc.Get(Loc.Stalled) + "]</color>";
        }

        private static string StationColor(Station station)
        {
            if (station.PlayerOwned)
            {
                return ColorSys.cyan;
            }
            int rank = PChar.GetRepRank(station.factionIndex);
            if (rank > 0)
            {
                return ColorSys.infoPos;
            }
            return rank < 0 ? ColorSys.infoNeg : ColorSys.neutral;
        }

        private static string FactionName(Station station)
        {
            if (station.PlayerOwned)
            {
                return Loc.Get(Loc.PlayerBase);
            }
            try
            {
                return FactionDB.GetFaction(station.factionIndex)?.factionName;
            }
            catch
            {
                return null;
            }
        }

        private static string FormatTime(float seconds)
        {
            if (seconds < 120f)
            {
                return Loc.Get(Loc.Seconds, seconds.ToString("0.#"));
            }
            if (seconds < 7200f)
            {
                return Loc.Get(Loc.Minutes, (seconds / 60f).ToString("0.#"));
            }
            return Loc.Get(Loc.Hours, (seconds / 3600f).ToString("0.#"));
        }
    }
}
