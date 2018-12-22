using Quartz;
using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mail;

namespace WindowsServiceCS
{
	public class ADJob2 : IJob
	{
		public ADJob2()
		{
		}

		public void Execute(IJobExecutionContext context)
		{
			this.WriteToFile("Tibs SMS Notify Service Job2");
			try
			{
				string constrj1 = ConfigurationManager.ConnectionStrings["constr"].ConnectionString;
				string ApiKey = string.Empty;
				string ApiuserName = string.Empty;
				string Apipassword = string.Empty;
				string CampaignId = string.Empty;
				string Routeid = string.Empty;
				string Senderid = string.Empty;
				string TimeFrom = string.Empty;
				string TimeTo = string.Empty;
				DataTable db1 = new DataTable();
				string query1 = "select * from [dbo].[NotificationManager] where Id = 1";
				using (SqlConnection con1 = new SqlConnection(constrj1))
				{
					using (SqlCommand cmd1 = new SqlCommand(query1))
					{
						cmd1.Connection = con1;
						using (SqlDataAdapter sda = new SqlDataAdapter(cmd1))
						{
							sda.Fill(db1);
						}
					}
				}
				foreach (DataRow datarow1 in db1.Rows)
				{
					ApiKey = datarow1["ApiKey"].ToString();
					ApiuserName = datarow1["ApiUsername"].ToString();
					Apipassword = datarow1["ApiPassword"].ToString();
					CampaignId = datarow1["CampaignId"].ToString();
					Routeid = datarow1["Routeid"].ToString();
					Senderid = datarow1["Senderid"].ToString();
					TimeFrom = datarow1["FromDate"].ToString();
					TimeTo = datarow1["ToDate"].ToString();
				}
				DateTime start = Convert.ToDateTime(TimeFrom);
				DateTime end = Convert.ToDateTime(TimeTo);
				DateTime now = DateTime.Now;
				DateTime date = DateTime.Now.Date;
				date = date.AddMinutes((double)start.Minute);
				DateTime strtdate = date.AddHours((double)start.Hour);
				date = DateTime.Now.Date;
				date = date.AddMinutes((double)end.Minute);
				DateTime endDate = date.AddHours((double)end.Hour);
				this.WriteToFile(string.Concat(new object[] { "Start :", strtdate, "End", endDate, "Current", now }));
				if (strtdate < endDate)
				{
					this.WriteToFile("Enter into Time Validation");
					if ((strtdate > now ? false : now <= endDate))
					{
						this.WriteToFile("Time Validation Success");
						this.WriteToFile(string.Concat(new object[] { "Start :", start.Hour, "End", end.Hour, "Current", now.Hour }));
						bool flag = false;
						string smsquery = string.Concat("select * from dbo.SmSDetail where IsSent='", flag.ToString(), "' or SmsStatus is null");
						DataTable smsTable = new DataTable();
						using (SqlConnection conj1 = new SqlConnection(constrj1))
						{
							using (SqlCommand cmdj1 = new SqlCommand(smsquery))
							{
								cmdj1.Connection = conj1;
								using (SqlDataAdapter sda = new SqlDataAdapter(cmdj1))
								{
									sda.Fill(smsTable);
								}
							}
						}
						foreach (DataRow smsrow in smsTable.Rows)
						{
							string mobileNumber = smsrow["MobileNumber"].ToString();
							string Message = smsrow["SMSBody"].ToString();
							string data = string.Concat(new string[] { "http://customers.smsmarketing.ae/app/smsapi/index.php?key=", ApiKey, "&campaign=", CampaignId, "&routeid=", Routeid, "&type=text&contacts=", mobileNumber, "&senderid=", Senderid, "&msg=", Message }) ?? "";
							try
							{
								try
								{
									string url = string.Format(data, new object[0]);
									string response = (new WebClient()).DownloadString(url);
									decimal.Parse(response, CultureInfo.InvariantCulture);
									SqlConnection conFinal = new SqlConnection(constrj1);
									conFinal.Open();
									string qurfinal = string.Concat(new object[] { "update dbo.SmSDetail set IsSent='", true.ToString(), "',SmsStatus='Delivered',LastModificationTime = GETDATE() where Id='", Convert.ToInt32(smsrow["Id"]), "' " });
									(new SqlCommand(qurfinal, conFinal)).ExecuteNonQuery();
									conFinal.Close();
								}
								catch (SmtpFailedRecipientsException smtpFailedRecipientsException)
								{
									SqlConnection errors = new SqlConnection(constrj1);
									errors.Open();
									string errfinal = string.Concat(new object[] { "update dbo.SmSDetail set IsSent='", true.ToString(), "',SmsStatus='Submitted',LastModificationTime= GETDATE() where Id='", Convert.ToInt32(smsrow["Id"]), "' " });
									(new SqlCommand(errfinal, errors)).ExecuteNonQuery();
									errors.Close();
								}
							}
							catch (Exception exception)
							{
								SqlConnection conFinal2 = new SqlConnection(constrj1);
								conFinal2.Open();
								string qurfinal2 = string.Concat(new object[] { "update dbo.SmSDetail set IsSent='", true.ToString(), "',SmsStatus='Delivered',LastModificationTime = GETDATE() where Id='", Convert.ToInt32(smsrow["Id"]), "' " });
								this.WriteToFile(string.Concat("Query: ", qurfinal2));
								(new SqlCommand(qurfinal2, conFinal2)).ExecuteNonQuery();
								conFinal2.Close();
							}
						}
					}
				}
			}
			catch (Exception exception1)
			{
				Exception ex = exception1;
				this.WriteToFile(string.Concat("Tibs Notify Service Job1 Error on: ", ex.Message, ex.StackTrace));
			}
		}

		private void WriteToFile(string text)
		{
			using (StreamWriter writer = new StreamWriter("C:\\LockedMail\\TibsNotifySmSJob2Log.txt", true))
			{
				DateTime now = DateTime.Now;
				writer.WriteLine(string.Format(text, now.ToString("yyyy/MM/dd hh:MM:ss tt")));
				writer.Close();
			}
		}
	}
}