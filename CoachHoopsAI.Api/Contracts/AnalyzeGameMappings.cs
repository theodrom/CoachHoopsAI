using CoachHoopsAI.Api.Models;
using CoachHoopsAI.Application.Models;
using CoachHoopsAI.Domain.Entities;
using CoachHoopsAI.Domain.Enums;
using CoachHoopsAI.Domain.GameContext;

namespace CoachHoopsAI.Api.Contracts
{
    public static class AnalyzeGameMappings
    {
        public static GameAnalysisInput ToGameAnalysisInput(this AnalyzeGameRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var level = ParseLevel(request.Level ?? string.Empty);

            var team = ToTeamStats(request.Team);
            var opponent = ToTeamStats(request.Opponent);

            var metadata = new GameMetadata(
                request.GameDate,
                string.IsNullOrWhiteSpace(request.TeamName) ? null : request.TeamName.Trim(),
                string.IsNullOrWhiteSpace(request.OpponentName) ? null : request.OpponentName.Trim(),
                string.IsNullOrWhiteSpace(request.Competition) ? null : request.Competition.Trim(),
                string.IsNullOrWhiteSpace(request.Season) ? null : request.Season.Trim(),
                string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim()
            );

            var gameFormat = ToGameFormat(request.GameFormat);
            var gameTiming = ToGameTiming(request.GameTiming);

            return new GameAnalysisInput(
                level,
                team,
                opponent,
                request?.Notes ?? string.Empty,
                metadata,
                request?.RulesProfile,
                gameFormat,
                gameTiming);

        }

        private static TeamStats ToTeamStats(TeamStatsDto? dto) => new()
        {
            Points = dto?.Points ?? 0,
            FieldGoalsMade = dto?.FieldGoalsMade ?? 0,
            FieldGoalsAttempted = dto?.FieldGoalsAttempted ?? 0,
            ThreePointsMade = dto?.ThreePointsMade ?? 0,
            ThreePointsAttempted = dto?.ThreePointsAttempted ?? 0,
            FreeThrowsMade = dto?.FreeThrowsMade ?? 0,
            FreeThrowsAttempted = dto?.FreeThrowsAttempted ?? 0,
            OffensiveRebounds = dto?.OffensiveRebounds ?? 0,
            DefensiveRebounds = dto?.DefensiveRebounds ?? 0,
            Assists = dto?.Assists ?? 0,
            Turnovers = dto?.Turnovers ?? 0,
            Steals = dto?.Steals ?? 0,
            Blocks = dto?.Blocks ?? 0,
            PersonalFouls = dto?.PersonalFouls ?? 0
        };

        private static GameFormat? ToGameFormat(GameFormatDto? dto)
        {
            if (dto == null)
                return null;

            return new GameFormat
            {
                RegulationPeriods = dto.RegulationPeriods,
                RegulationPeriodMinutes = dto.RegulationPeriodMinutes,
                OvertimePeriodMinutes = dto.OvertimePeriodMinutes,
                Name = string.IsNullOrWhiteSpace(dto.Name) ? null : dto.Name.Trim()
            };
        }

        private static GameTiming? ToGameTiming(GameTimingDto? dto)
        {
            if (dto == null)
                return null;

            return new GameTiming
            {
                CurrentPeriod = dto.CurrentPeriod,
                ClockRemaining = TimeSpan.FromSeconds(dto.ClockRemainingSeconds)
            };
        }

        public static AnalyzeGameResponse ToResponseDto(this GameAnalysisResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var problemTags = result.ProblemTags?
                .Select(t => t.ToString())
                .ToList();

            var offense = result.Suggestions?
                .Where(s => s.Category == SuggestionCategory.Offense)
                .Select(s => new SuggestionItemDto()
                {
                    Suggestion = s.Text,
                    Reason = s.Reason
                })
                .ToList();

            var defense = result.Suggestions?
                .Where(s => s.Category == SuggestionCategory.Defense)
                .Select(s => new SuggestionItemDto()
                {
                    Suggestion = s.Text,
                    Reason = s.Reason
                })
                .ToList();

            var other = result.Suggestions?
                .Where(s => s.Category == SuggestionCategory.Other)
                .Select(s => new SuggestionItemDto()
                {
                    Suggestion = s.Text,
                    Reason = s.Reason
                })
                .ToList();

            return new AnalyzeGameResponse
            {
                ProblemTags = problemTags,
                Suggestions = new SuggestionsResponse
                {
                    Offense = offense ?? [],
                    Defense = defense ?? [],
                    Other = other ?? []
                },
                Diagnostics = result.Diagnostics == null ? new GameDiagnosticsDto() : new GameDiagnosticsDto
                {
                    PointsDiff = result.Diagnostics.PointsDiff,
                    TurnoversDiff = result.Diagnostics.TurnoversDiff,
                    OffensiveReboundsDiff = result.Diagnostics.OffensiveReboundsDiff,
                    DefensiveReboundsDiff = result.Diagnostics.DefensiveReboundsDiff,
                    ThreePointPctDiff = result.Diagnostics.ThreePointPctDiff,
                    ThreePointAttemptsDiff = result.Diagnostics.ThreePointAttemptsDiff,
                    FoulsDiff = result.Diagnostics.FoulsDiff,
                    TeamFieldGoalPercentage = result.Diagnostics.TeamFieldGoalPercentage,
                    OpponentFieldGoalPercentage = result.Diagnostics.OpponentFieldGoalPercentage,
                    FieldGoalPctDiff = result.Diagnostics.FieldGoalPctDiff,
                    AppliedRulesProfile = result.Diagnostics.AppliedRulesProfile

                }
            };
        }

        private static Level ParseLevel(string level)
        {
            if (string.IsNullOrWhiteSpace(level))
                return Level.Amateur; // sensible default

            var normalized = level.Replace(" ", "", StringComparison.OrdinalIgnoreCase);

            // handle common variants
            if (normalized.Equals("easybasket", StringComparison.OrdinalIgnoreCase))
                return Level.EasyBasket;

            if (normalized.Equals("youth", StringComparison.OrdinalIgnoreCase))
                return Level.Youth;

            if (normalized.Equals("pro", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("professional", StringComparison.OrdinalIgnoreCase))
                return Level.Pro;

            if (normalized.Equals("amateur", StringComparison.OrdinalIgnoreCase))
                return Level.Amateur;

            // fallback: try enum parse
            if (Enum.TryParse<Level>(level, true, out var parsed))
                return parsed;

            return Level.Amateur;
        }
    }
}
