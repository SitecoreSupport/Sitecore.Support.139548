using System.Linq;
using Sitecore.Analytics;
using Sitecore.Analytics.Data.Items;
using Sitecore.Cintel;
using Sitecore.Cintel.Commons;
using Sitecore.Cintel.Reporting;
using Sitecore.Cintel.Reporting.Processors;

namespace Sitecore.Support.Cintel.Reporting.Contact.ProfileInfo.Processors
{
    using Sitecore.Globalization;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Sitecore.Cintel.Configuration;
    using Sitecore.Cintel.Reporting.Contact.ProfilePatternMatch;
    using Sitecore.Diagnostics;

    public class FindBestPatternMatchAndApplyToProfileInfo : ReportProcessorBase
    {
        #region Public methods

        public override void Process(ReportProcessorArgs args)
        {
            DataTable resultTable = GrabPreliminaryResultsFromCurrentReport(args);
            Assert.IsNotNull(resultTable, "Result table for {0} could not be found.", args.ReportParameters.ViewName);

            this.ApplyPatternsToResultTable(args, resultTable);
        }

        #endregion

        #region Private methods

        private static ViewParameters GetParametersForRetrievingBestPattern(ReportProcessorArgs args, DataRow row)
        {
            var viewArgs = new ViewParameters
            {
                SortFields = new List<SortCriterion>
                {
                  new SortCriterion(Schema.PatternGravityShare.Name, SortDirection.Desc)
                },
                PageSize = 1,
                ViewName = "profile-pattern-matches",
                ViewEntityId = row.Field<Guid>(Sitecore.Cintel.Reporting.Contact.ProfileInfo.Schema.ProfileId.Name).ToString(),
                ContactId = args.ReportParameters.ContactId,
                AdditionalParameters = new Dictionary<string, object>
                {
                  {
                    WellknownParameters.VisitId, row.Field<Guid>(Sitecore.Cintel.Reporting.Contact.ProfileInfo.Schema.LatestVisitId.Name).ToString()
                  }
                }
            };
            return viewArgs;
        }

        private static DataTable GrabPreliminaryResultsFromCurrentReport(ReportProcessorArgs args)
        {
            return args.ResultTableForView;
        }

        private bool ApplyPatternToOneProfile(ReportProcessorArgs args, DataRow profileRow)
        {
            bool allPatternsApplied = true;
            var profileId = profileRow.Field<Guid>(Sitecore.Cintel.Reporting.Contact.ProfileInfo.Schema.ProfileId.Name);

            if (profileId == Guid.Empty)
            {
                allPatternsApplied = false;
            }
            #region Sitecore Support Fix
            ProfileItem profileFromSitecore = Tracker.DefinitionItems.Profiles[profileId];
            if (profileFromSitecore.Patterns == null || !profileFromSitecore.Patterns.Any())
            {
                return false;
            }
            #endregion
            ViewParameters bestPatternMatchParameters = GetParametersForRetrievingBestPattern(args, profileRow);

            DataTable bestPatternMatch = CustomerIntelligenceManager.ViewProvider.GenerateContactView(bestPatternMatchParameters).Data.Dataset[bestPatternMatchParameters.ViewName];

            if (bestPatternMatch != null && bestPatternMatch.Rows.Count > 0)
            {
                if (!(this.TryFillData(profileRow, Sitecore.Cintel.Reporting.Contact.ProfileInfo.Schema.BestMatchedPatternId, bestPatternMatch.Rows[0], Schema.PatternId.Name)
                && this.TryFillData(profileRow, Sitecore.Cintel.Reporting.Contact.ProfileInfo.Schema.BestMatchedPatternDisplayName, bestPatternMatch.Rows[0], Schema.PatternDisplayName.Name)
                && this.TryFillData(profileRow, Sitecore.Cintel.Reporting.Contact.ProfileInfo.Schema.BestMatchedPatternGravityShare, bestPatternMatch.Rows[0], Schema.PatternGravityShare.Name)))
                {
                    allPatternsApplied = false;
                }
            }
            else
            {
                string message = Globalization.Translate.Text(WellknownTexts.ProfileDataCannotBeShown);
                throw new ApplicationException(message);
            }

            return allPatternsApplied;
        }

        private void ApplyPatternsToResultTable(ReportProcessorArgs args, DataTable resultTable)
        {
            bool patternMissing = false;

            foreach (DataRow profileRow in resultTable.AsEnumerable())
            {
                patternMissing = !this.ApplyPatternToOneProfile(args, profileRow);
            }

            if (patternMissing)
            {
                LogNotificationForView(args.ReportParameters.ViewName, MandatoryDataMissing);
            }
        }
        internal static NotificationMessage MandatoryDataMissing
        {
            get
            {
                return new NotificationMessage()
                {
                    Id = 13,
                    MessageType = NotificationTypes.Warning,
                    Text =
                        Sitecore.Globalization.Translate.Text(
                            WellknownTexts.OneOrMoreDataEntriesAreMissingDueToInvalidData)
                };
            }
        }
        #endregion
    }
}