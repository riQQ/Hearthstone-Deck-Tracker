#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Hearthstone_Deck_Tracker.Annotations;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.Hearthstone;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Helpers;
using LiveCharts.Wpf;

#endregion

namespace Hearthstone_Deck_Tracker.Stats.CompiledStats
{
	public class ArenaStats : INotifyPropertyChanged
	{
		public static ArenaStats Instance { get; } = new ArenaStats();

		private IEnumerable<Deck> ArenaDecks => !Core.Initialized ? new List<Deck>() : DeckList.Instance.Decks.Where(x => x != null && x.IsArenaDeck);

		public IEnumerable<ArenaRun> Runs => ArenaDecks.Select(x => new ArenaRun(x)).OrderByDescending(x => x.StartTime);

		public IEnumerable<ClassStats> ClassStats => GetFilteredRuns(classFilter: false).GroupBy(x => x.Class).Select(x => new ClassStats(x.Key, x)).OrderBy(x => x.Class);

		public int PacksCountClassic => GetFilteredRuns().Sum(x => x.Packs.Count(p => p == ArenaRewardPacks.Classic));

		public int PacksCountGvg => GetFilteredRuns().Sum(x => x.Packs.Count(p => p == ArenaRewardPacks.GoblinsVsGnomes));

		public int PacksCountTgt => GetFilteredRuns().Sum(x => x.Packs.Count(p => p == ArenaRewardPacks.TheGrandTournament));

		public int PacksCountWotog => GetFilteredRuns().Sum(x => x.Packs.Count(p => p == ArenaRewardPacks.WhispersOfTheOldGods));

		public int PacksCountMsg => GetFilteredRuns().Sum(x => x.Packs.Count(p => p == ArenaRewardPacks.MeanStreetsOfGadgetzan));

		public int PacksCountUngoro => GetFilteredRuns().Sum(x => x.Packs.Count(p => p == ArenaRewardPacks.JourneyToUngoro));

		public int PacksCountIcecrown => GetFilteredRuns().Sum(x => x.Packs.Count(p => p == ArenaRewardPacks.KnightsOfTheFrozenThrone));

		public int PacksCountLoot => GetFilteredRuns().Sum(x => x.Packs.Count(p => p == ArenaRewardPacks.Loot));

		public int PacksCountTotal => GetFilteredRuns().Sum(x => x.PackCount);

		public double PacksCountAveragePerRun => Math.Round(GetFilteredRuns(requireAnyReward: true).Select(x => x.PackCount).DefaultIfEmpty(0).Average(), 2);

		public int GoldTotal => GetFilteredRuns().Sum(x => x.Gold);

		public double GoldAveragePerRun => Math.Round(GetFilteredRuns(requireAnyReward: true).Select(x => x.Gold).DefaultIfEmpty(0).Average(), 2);

		public int GoldSpent => GetFilteredRuns().Count(x => x.Deck.ArenaReward.PaymentMethod == ArenaPaymentMethod.Gold) * 150;

		public int DustTotal => GetFilteredRuns().Sum(x => x.Dust);

		public double DustAveragePerRun => Math.Round(GetFilteredRuns(requireAnyReward: true).Select(x => x.Dust).DefaultIfEmpty(0).Average(), 2);

		public int CardCountTotal => GetFilteredRuns().Sum(x => x.CardCount);

		public double CardCountAveragePerRun => Math.Round(GetFilteredRuns(requireAnyReward: true).Select(x => x.CardCount).DefaultIfEmpty(0).Average(), 2);

		public int CardCountGolden => GetFilteredRuns().Sum(x => x.CardCountGolden);

		public double CardCountGoldenAveragePerRun => Math.Round(GetFilteredRuns(requireAnyReward: true).Select(x => x.CardCountGolden).DefaultIfEmpty(0).Average(), 2);

		public ClassStats ClassStatsBest => !ClassStats.Any() ? null : ClassStats.OrderByDescending(x => x.WinRate).First();
		public ClassStats ClassStatsWorst => !ClassStats.Any() ? null : ClassStats.OrderBy(x => x.WinRate).First();
		public ClassStats ClassStatsMostPicked => !ClassStats.Any() ? null : ClassStats.OrderByDescending(x => x.Runs).First();
		public ClassStats ClassStatsLeastPicked => !ClassStats.Any() ? null : ClassStats.OrderBy(x => x.Runs).First();

		public ClassStats ClassStatsDruid => GetClassStats("Druid");
		public ClassStats ClassStatsHunter => GetClassStats("Hunter");
		public ClassStats ClassStatsMage => GetClassStats("Mage");
		public ClassStats ClassStatsPaladin => GetClassStats("Paladin");
		public ClassStats ClassStatsPriest => GetClassStats("Priest");
		public ClassStats ClassStatsRogue => GetClassStats("Rogue");
		public ClassStats ClassStatsShaman => GetClassStats("Shaman");
		public ClassStats ClassStatsWarlock => GetClassStats("Warlock");
		public ClassStats ClassStatsWarrior => GetClassStats("Warrior");

		public ClassStats ClassStatsAll => GetFilteredRuns(classFilter: false).GroupBy(x => true).Select(x => new ClassStats("All", x)).FirstOrDefault();

		public int RunsCount => GetFilteredRuns().Count();
		public int GamesCountTotal => GetFilteredRuns().Sum(x => x.Games.Count());
		public int GamesCountWon => GetFilteredRuns().Sum(x => x.Games.Count(g => g.Result == GameResult.Win));
		public int GamesCountLost => GetFilteredRuns().Sum(x => x.Games.Count(g => g.Result == GameResult.Loss));

		public double AverageWinsPerRun => (double)GamesCountWon / GetFilteredRuns().Count();

		public IEnumerable<ArenaRun> FilteredRuns => GetFilteredRuns();

			public SeriesCollection PlayedClassesPercent => GetFilteredRuns()
			.GroupBy(x => x.Class)
			.OrderBy(x => x.Key)
			.Select(
				    x =>
					new PieSeries
					{
						Title = x.Key + " (" + Math.Round(100.0 * x.Count() / RunsCount) + "%)",
						Values = new ChartValues<ObservableValue> { new ObservableValue(x.Count()) },
						Fill = new SolidColorBrush(Helper.GetClassColor(x.Key, true)),
						DataLabels = true,
						Foreground = Brushes.Black
					}).AsSeriesCollection();

		public SeriesCollection OpponentClassesPercent
		{
			get
			{
				var opponents = GetFilteredRuns().SelectMany(x => x.Deck.DeckStats.Games.Select(g => g.OpponentHero)).ToList();
				return
					opponents.GroupBy(x => x)
					         .OrderBy(x => x.Key)
					         .Select(
					                 g =>
									 new PieSeries
									 {
										 Title = g.Key + " (" + Math.Round(100.0 * g.Count() / opponents.Count()) + "%)",
										 Values = new ChartValues<ObservableValue> { new ObservableValue(g.Count()) },
										 Fill = new SolidColorBrush(Helper.GetClassColor(g.Key, true)),
										 DataLabels = true,
										 Foreground = Brushes.Black
									 }).AsSeriesCollection();
			}
		}

		public SeriesCollection Wins
		{
			get
			{
				var groupedByWins =
					GetFilteredRuns()
						.GroupBy(x => x.Deck.DeckStats.Games.Count(g => g.Result == GameResult.Win))
						.Select(x => new {Wins = x.Key, Count = x.Count(), Runs = x})
						.ToList();
				return new SeriesCollection
				{
					new ColumnSeries
					{
						Title = "Runs",
						Values = new ChartValues<ObservableValue>(Enumerable.Range(0, 13).Select(n =>
						{
							var runs = groupedByWins.FirstOrDefault(x => x.Wins == n);
							if (runs == null)
								return new ObservableValue(0);
							return new ObservableValue(runs.Count);
						})),
						DataLabels = true,
					}
				}; ;
			}
		}

		public SeriesCollection WinsByClass
		{
			get
			{
				return GetFilteredRuns()
						.GroupBy(x => x.Class)
						.Select(x =>
						{
							var groupedByWins = x.GroupBy(y => y.Deck.DeckStats.Games.Count(g => g.Result == GameResult.Win));
							var @class = x.Key;

							var countOfRunsWithNWins = Enumerable.Range(0, 13).Select(n =>
							{
								var xWinRuns = groupedByWins.FirstOrDefault(r => r.Key == n);
								if (xWinRuns == null)
									return 0.0;
								else
									return (double)xWinRuns.Count();
							});
							var values = new ChartValues<double>(countOfRunsWithNWins);
							return new StackedColumnSeries
							{
								Title = @class,

								Values = values,
								Fill = new SolidColorBrush(Helper.GetClassColor(x.Key, true)),

								StackMode = StackMode.Values, // this is not necessary, values is the default stack mode
								DataLabels = true

							};
						})
						.AsSeriesCollection();
			}
		}

		

		public IEnumerable<ChartStats>[] WinLossVsClass
		{
			get
			{
				var gamesGroupedByOppHero = GetFilteredRuns().SelectMany(x => x.Deck.DeckStats.Games).GroupBy(x => x.OpponentHero);
				return Enum.GetNames(typeof(HeroClass)).Select(x =>
				{
					var classGames = gamesGroupedByOppHero.FirstOrDefault(g => g.Key == x);
					if(classGames == null)
						return new[] {new ChartStats {Name = x, Value = 0, Brush = new SolidColorBrush()}};
					return classGames.GroupBy(g => g.Result).OrderBy(g => g.Key).Select(g =>
					{
						var color = Helper.GetClassColor(x, true);
						if(g.Key == GameResult.Loss)
							color = Color.FromRgb((byte)(color.R * 0.7), (byte)(color.G * 0.7), (byte)(color.B * 0.7));
						return new ChartStats {Name = g.Key.ToString() + " vs " + x.ToString(), Value = g.Count(), Brush = new SolidColorBrush(color)};
					});
				}).ToArray();
			}
		}

		public SeriesCollection AvgWinsPerClass
		{
			get
			{
				return
					GetFilteredRuns()
						.GroupBy(x => x.Class)
						.Select(
						        x =>
						        new ColumnSeries
								{
									Title = x.Key,
									Values = new ChartValues<ObservableValue> { new ObservableValue(Math.Round((double)x.Sum(d => d.Deck.DeckStats.Games.Count(g => g.Result == GameResult.Win)) / x.Count(), 1)) },
									Fill = new SolidColorBrush(Helper.GetClassColor(x.Key, true)),
									DataLabels = true,
								})
						.OrderBy(x => ((ChartValues<ObservableValue>) x.Values).First().Value).AsSeriesCollection();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public ClassStats GetClassStats(string @class)
		{
			var runs = GetFilteredRuns(classFilter: false).Where(x => x.Class == @class).ToList();
			return !runs.Any() ? null : new ClassStats(@class, runs);
		}

		public IEnumerable<ArenaRun> GetFilteredRuns(bool archivedFilter = true, bool classFilter = true, bool regionFilter = true,
		                                             bool timeframeFilter = true, bool requireAnyReward = false)
		{
			var filtered = Runs;
			if(requireAnyReward)
				filtered = filtered.Where(x => x.PackCount > 0 || x.Gold > 0 || x.Dust > 0 || x.CardCount > 0);
			if (archivedFilter && !Config.Instance.ArenaStatsIncludeArchived)
				filtered = filtered.Where(x => !x.Deck.Archived);
			if(classFilter && Config.Instance.ArenaStatsClassFilter != HeroClassStatsFilter.All)
				filtered = filtered.Where(x => x.Class == Config.Instance.ArenaStatsClassFilter.ToString());
			if(regionFilter && Config.Instance.ArenaStatsRegionFilter != RegionAll.ALL)
			{
				var region = (Region)Enum.Parse(typeof(Region), Config.Instance.ArenaStatsRegionFilter.ToString());
				filtered = filtered.Where(x => x.Games.Any(g => g.Region == region));
			}
			if(timeframeFilter)
			{
				switch(Config.Instance.ArenaStatsTimeFrameFilter)
				{
					case DisplayedTimeFrame.AllTime:
						break;
					case DisplayedTimeFrame.CurrentSeason:
						filtered = filtered.Where(g => g.StartTime >= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
						break;
					case DisplayedTimeFrame.LastSeason:
						filtered = filtered.Where(g => g.StartTime >= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1)
													&& g.StartTime < new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
						break;
					case DisplayedTimeFrame.CustomSeason:
						var current = Helper.CurrentSeason;
						filtered = filtered.Where(g => g.StartTime >= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
																		.AddMonths(Config.Instance.ArenaStatsCustomSeasonMin - current));
						if(Config.Instance.ArenaStatsCustomSeasonMax.HasValue)
						{
							filtered = filtered.Where(g => g.StartTime < new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
																		.AddMonths(Config.Instance.ArenaStatsCustomSeasonMax.Value - current + 1));
						}
						break;
					case DisplayedTimeFrame.ThisWeek:
						filtered = filtered.Where(g => g.StartTime > DateTimeHelper.StartOfWeek);
						break;
					case DisplayedTimeFrame.Today:
						filtered = filtered.Where(g => g.StartTime > DateTime.Today);
						break;
					case DisplayedTimeFrame.Custom:
						var start = (Config.Instance.ArenaStatsTimeFrameCustomStart ?? DateTime.MinValue).Date;
						var end = (Config.Instance.ArenaStatsTimeFrameCustomEnd ?? DateTime.MaxValue).Date;
						filtered = filtered.Where(g => g.EndTime.Date >= start && g.EndTime.Date <= end);
						break;
				}
			}
			return filtered;
		}

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void OnPropertyChanged(string[] properties)
		{
			foreach(var prop in properties)
				OnPropertyChanged(prop);
		}

		public void UpdateArenaStats()
		{
			OnPropertyChanged(nameof(Runs));
			OnPropertyChanged(nameof(OpponentClassesPercent));
			OnPropertyChanged(nameof(PlayedClassesPercent));
			OnPropertyChanged(nameof(Wins));
			OnPropertyChanged(nameof(AvgWinsPerClass));
			OnPropertyChanged(nameof(FilteredRuns));
		}

		public void UpdateArenaStatsHighlights()
		{
			OnPropertyChanged(nameof(ClassStats));
			OnPropertyChanged(nameof(ClassStatsDruid));
			OnPropertyChanged(nameof(ClassStatsHunter));
			OnPropertyChanged(nameof(ClassStatsMage));
			OnPropertyChanged(nameof(ClassStatsPaladin));
			OnPropertyChanged(nameof(ClassStatsPriest));
			OnPropertyChanged(nameof(ClassStatsRogue));
			OnPropertyChanged(nameof(ClassStatsShaman));
			OnPropertyChanged(nameof(ClassStatsWarlock));
			OnPropertyChanged(nameof(ClassStatsWarrior));
			OnPropertyChanged(nameof(ClassStatsAll));
			OnPropertyChanged(nameof(ClassStatsBest));
			OnPropertyChanged(nameof(ClassStatsWorst));
			OnPropertyChanged(nameof(ClassStatsMostPicked));
			OnPropertyChanged(nameof(ClassStatsLeastPicked));
		}

		public void UpdateArenaRewards()
		{
			OnPropertyChanged(nameof(GoldTotal));
			OnPropertyChanged(nameof(GoldAveragePerRun));
			OnPropertyChanged(nameof(GoldSpent));
			OnPropertyChanged(nameof(DustTotal));
			OnPropertyChanged(nameof(DustAveragePerRun));
			OnPropertyChanged(nameof(PacksCountClassic));
			OnPropertyChanged(nameof(PacksCountGvg));
			OnPropertyChanged(nameof(PacksCountTgt));
			OnPropertyChanged(nameof(PacksCountWotog));
			OnPropertyChanged(nameof(PacksCountMsg));
			OnPropertyChanged(nameof(PacksCountUngoro));
			OnPropertyChanged(nameof(PacksCountIcecrown));
			OnPropertyChanged(nameof(PacksCountLoot));
			OnPropertyChanged(nameof(PacksCountTotal));
			OnPropertyChanged(nameof(PacksCountAveragePerRun));
			OnPropertyChanged(nameof(CardCountTotal));
			OnPropertyChanged(nameof(CardCountGolden));
			OnPropertyChanged(nameof(CardCountAveragePerRun));
			OnPropertyChanged(nameof(CardCountGoldenAveragePerRun));
		}

		public void UpdateArenaRuns()
		{
			OnPropertyChanged(nameof(Runs));
			OnPropertyChanged(nameof(FilteredRuns));
		}

		public void UpdateExpensiveArenaStats()
		{
			OnPropertyChanged(nameof(WinLossVsClass));
			OnPropertyChanged(nameof(WinsByClass));
		}
	}
}
