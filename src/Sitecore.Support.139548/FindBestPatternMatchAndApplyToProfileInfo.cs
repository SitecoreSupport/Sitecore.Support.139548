using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Cintel;
using Sitecore.Cintel.Commons;
using Sitecore.Cintel.Reporting;
using Sitecore.Cintel.Reporting.Processors;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using System.Data;

namespace Sitecore.Support.Cintel.Reporting.Contact.ProfileInfo.Processors
{
    public class FindBestPatternMatchAndApplyToProfileInfo : ReportProcessorBase
    {
        // Methods
        private void ApplyPatternsToResultTable(ReportProcessorArgs args, DataTable resultTable)
        {
            bool flag = false;
            foreach(DataRow row in resultTable.AsEnumerable())
            {
                flag = !this.ApplyPatternToOneProfile(args, row);
            }
            if(flag)
            {
                var mandatoryDataMissing = new NotificationMessage
                {
                    Id = 13,
                    MessageType = NotificationTypes.Error,
                    Text = Translate.Text("One or more data entries are missing due to invalid data")
                };
                ReportProcessorBase.LogNotificationForView(args.ReportParameters.ViewName, mandatoryDataMissing);
            }
        }

        private bool ApplyPatternToOneProfile(ReportProcessorArgs args, DataRow profileRow)
        {
            bool flag = true;
            if(profileRow.Field<Guid>(Sitecore.Cintel.Reporting.Contact.ProfileInfo.Schema.ProfileId.Name) == Guid.Empty)
            {
                flag = false;
            }
            ViewParameters parametersForRetrievingBestPattern = GetParametersForRetrievingBestPattern(args, profileRow);
            DataTable table = CustomerIntelligenceManager.ViewProvider.GenerateContactView(parametersForRetrievingBestPattern).Data.Dataset[parametersForRetrievingBestPattern.ViewName];
            if((table == null) || (table.Rows.Count <= 0))
            {
                throw new ApplicationException(Translate.Text("Profile data can not be shown for this visitor. Contact the system administrator."));
            }
            return (((base.TryFillData<Guid>(profileRow, Sitecore.Cintel.Reporting.Contact.ProfileInfo.Schema.BestMatchedPatternId, table.Rows[0], Sitecore.Cintel.Reporting.Contact.ProfilePatternMatch.Schema.PatternId.Name) && base.TryFillData<string>(profileRow, Sitecore.Cintel.Reporting.Contact.ProfileInfo.Schema.BestMatchedPatternDisplayName, table.Rows[0], Sitecore.Cintel.Reporting.Contact.ProfilePatternMatch.Schema.PatternDisplayName.Name)) && base.TryFillData<double>(profileRow, Sitecore.Cintel.Reporting.Contact.ProfileInfo.Schema.BestMatchedPatternGravityShare, table.Rows[0], Sitecore.Cintel.Reporting.Contact.ProfilePatternMatch.Schema.PatternGravityShare.Name)) && flag);
        }

        private static ViewParameters GetParametersForRetrievingBestPattern(ReportProcessorArgs args, DataRow row)
        {
            ViewParameters parameters2 = new ViewParameters();
            List<SortCriterion> list = new List<SortCriterion> {
                new SortCriterion(Sitecore.Cintel.Reporting.Contact.ProfilePatternMatch.Schema.PatternGravityShare.Name, SortDirection.Desc)
            };
            parameters2.SortFields = list;
            parameters2.PageSize = 1;
            parameters2.ViewName = "profile-pattern-matches";
            parameters2.ViewEntityId = row.Field<Guid>(Sitecore.Cintel.Reporting.Contact.ProfileInfo.Schema.ProfileId.Name).ToString();
            parameters2.ContactId = args.ReportParameters.ContactId;
            Dictionary<string, object> dictionary = new Dictionary<string, object> {
                {
                    "VisitId",
                    row.Field<Guid>(Sitecore.Cintel.Reporting.Contact.ProfileInfo.Schema.LatestVisitId.Name).ToString()
                }
            };
            parameters2.AdditionalParameters = dictionary;
            return parameters2;
        }

        private static DataTable GrabPreliminaryResultsFromCurrentReport(ReportProcessorArgs args) =>
            args.ResultTableForView;

        public override void Process(ReportProcessorArgs args)
        {
            DataTable table = GrabPreliminaryResultsFromCurrentReport(args);
            Assert.IsNotNull(table, "Result table for {0} could not be found.", new object[] { args.ReportParameters.ViewName });
            this.ApplyPatternsToResultTable(args, table);
        }
    }
}