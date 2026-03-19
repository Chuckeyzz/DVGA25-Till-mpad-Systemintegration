using DVGA25_Datatransformer;
using Newtonsoft.Json;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using static Org.BouncyCastle.Math.Primes;
using File = System.IO.File;

namespace DATATRANFORMERSERVICE
{
	internal class dataTransformerService
	{
		private Producer _producer;
		private Consumer _consumer;

		public dataTransformerService() {
			_producer = new Producer();
			_consumer = new Consumer();
			
			//creating directories for storage
			string basePath = AppContext.BaseDirectory;

			Directory.CreateDirectory(Path.Combine(basePath, "Data"));
			Directory.CreateDirectory(Path.Combine(basePath, "Logs"));
			Directory.CreateDirectory(Path.Combine(basePath, "Errors"));
			Directory.CreateDirectory(Path.Combine(basePath, "Logs", "HR"));
			Directory.CreateDirectory(Path.Combine(basePath, "Logs", "INTERMEDIATE"));
			Directory.CreateDirectory(Path.Combine(basePath, "Logs", "TIME"));
			Directory.CreateDirectory(Path.Combine(basePath, "Logs", "SALARY"));
		}

		public async void winformsProcess() {

		}
		
		private async Task transformIntermediateToXML_Time()
		{
			string kauID = "peremil100";
			string fileDateTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
			string logPath = @"Logs\TIME";
			string logFilePathAndName = logPath + "time_" + kauID + '_' + fileDateTime + ".xml";

			try
			{
				XmlDocument doc = new();
				doc.Load(@"Data\intermediate.xml");
				XmlNodeList? entities = doc.SelectNodes("/Employees/Employee"); //hämta ut alla "Employee"-element

				XElement employees = new XElement("employees"); //"Root"-elementet för ut-filen
				if (entities is not null && entities.Count > 0)
				{
					foreach (XmlNode entity in entities) //loopa genom alla "Employee"-element i in-filen
					{
						string employee_id = entity["EmpID"]!.InnerText;
						string name = entity["Firstname"]!.InnerText + " " + entity["Lastname"]!.InnerText;
						string extent = entity["EmpExtent"]!.InnerText;
						string designation = entity["Designation"]!.InnerText;
						string department = entity["Department"]!.InnerText;
						string join_date = entity["Joindate"]!.InnerText;
						string email = entity["Email"]!.InnerText;
						string birthdate = entity["Birthdate"]!.InnerText;
						string shortname = (entity["Firstname"]!.InnerText.Substring(0, 3) +
											entity["Lastname"]!.InnerText.Substring(0, 3));

						//calculate age from birthyear
						int birthyear = 0, age = 0;
						if (int.TryParse(birthdate.Substring(0, 4), out birthyear)) { age = 2026 - birthyear; }
						else { age = 0; }

						XElement employee = new XElement("employee"); //barn till "Root"-elementet ("Employees)
						employee.Add(new XElement("employee_id", employee_id));
						employee.Add(new XElement("name", name));
						employee.Add(new XElement("extent", extent));
						employee.Add(new XElement("designation", designation));
						employee.Add(new XElement("department", department));
						employee.Add(new XElement("join_date", join_date));
						employee.Add(new XElement("email", email));
						employee.Add(new XElement("age", age));
						employee.Add(new XElement("shortname", shortname));

						employees.Add(employee);
					}
				}

				//rtbOutput.Text = employees.ToString();

				string pathAndFileName = @"Data\time.xml";
				employees.Save(pathAndFileName);
				employees.Save(logFilePathAndName);
				transform_XMLToJson();
			}
			catch (Exception e)
			{
				
				string errorLogPath = @"Errors\TimeErrors.txt";
				File.WriteAllText(errorLogPath, "Error in formIntermediateToXML_App: Exception: " + e.Message + "\n");
			}
		}
		private async Task transformIntermediateToXML_Salary()
		{
			string kauID = "peremil100";
			string fileDateTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
			string logPath = @"Logs\SALARY\";
			string logFilePathAndName = logPath + "salary_" + kauID + '_' + fileDateTime + ".xml";

			try
			{
				XmlDocument doc = new();
				doc.Load(@"Data/intermediate.xml");
				XmlNodeList? entities = doc.SelectNodes("/Employees/Employee"); //hämta ut alla "Employee"-element 
				XElement employees = new XElement("employees"); //"Root"-elementet för ut-filen
				if (entities is not null && entities.Count > 0)
				{
					foreach (XmlNode entity in entities) //loopa genom alla "Employee"-element i in-filen
					{
						string employee_id = entity["EmpID"]!.InnerText;
						string firstname = entity["Firstname"]!.InnerText;
						string lastname = entity["Lastname"]!.InnerText;
						string address = entity["StreetAddress"]!.InnerText;
						string city = entity["City"]!.InnerText;
						string extent = entity["EmpExtent"]!.InnerText;
						string annualSalary = entity["AnnualSalary"]!.InnerText;

						int taxcode = 34;

						int monthly_salary = 0, yearlySalary = -1, tax_reduction = 0;
						if (!int.TryParse(annualSalary, out yearlySalary))
						{
							//rtbOutput.Text += "Error Converting salary to integer value!";
							return;
						}

						//If yearly salary doesn't exist don't calculate tax reduction.
						if (yearlySalary != -1)
						{
							monthly_salary = yearlySalary / 12;
							tax_reduction = await calculateTaxReductionAsync(taxcode, monthly_salary); //TODO GET FROM SKATTEVERKET API
						}

						string bank_name = entity["Bank"]!.InnerText;
						string bank_account = entity["Account_no"]!.InnerText;
						string vacation_days = entity["Vacation_days"]!.InnerText;
						string phone = entity["Phone"]!.InnerText;
						string email = entity["Email"]!.InnerText;
						string birth_date = entity["Birthdate"]!.InnerText;
						string personal_id = entity["PersonID"]!.InnerText;

						XElement employee = new XElement("employee"); //barn till "Root"-elementet ("Employees)
						employee.Add(new XElement("employee_id", employee_id));
						employee.Add(new XElement("firstname", firstname));
						employee.Add(new XElement("lastname", lastname));
						employee.Add(new XElement("address", address));
						employee.Add(new XElement("city", city));
						employee.Add(new XElement("extent", extent));
						employee.Add(new XElement("taxcode", taxcode));
						employee.Add(new XElement("monthly_salary", monthly_salary));
						employee.Add(new XElement("tax_reduction", tax_reduction));
						employee.Add(new XElement("bank_name", bank_name));
						employee.Add(new XElement("bank_account", bank_account));
						employee.Add(new XElement("vacation_days", vacation_days));
						employee.Add(new XElement("phone", phone));
						employee.Add(new XElement("email", email));
						employee.Add(new XElement("birth_date", birth_date));
						employee.Add(new XElement("personal_id", personal_id));

						employees.Add(employee);
					}
				}
				string pathAndFileName = @"Data\salary.xml";

				employees.Save(pathAndFileName);
				employees.Save(logFilePathAndName);
			}
			catch (Exception e)
			{
				
				string errorLogPath = @"Logs\INTERMEDIATE\IntermediateErrors.txt";
				File.WriteAllText(errorLogPath, "Error in formIntermediateToXML_App: Exception: " + e.Message + "\n"); 
			}
		}
		private async Task<int> calculateTaxReductionAsync(int taxCode, int monthlySalary)
		{
			int totalTaxReduction = -1;
			int fromSalaryRange = 0, toSalaryRange = 0;

			string uriString =
					//URI for API call
					"https://skatteverket.entryscape.net/rowstore/dataset/88320397-5c32-4c16-ae79-d36d95b17b95/json" +
					$"?tabellnr={taxCode}" +
					"&%C3%A5r=2026" +
					"&antal%20dgr=30B" +
					"&_limit=500&_offset=100";

			using var http = new HttpClient();
			var jsondoc = await http.GetStringAsync(uriString);
			using JsonDocument doc = JsonDocument.Parse(jsondoc);

			foreach (JsonElement row in doc.RootElement.GetProperty("results").EnumerateArray())
			{
				if (!int.TryParse((row.GetProperty("inkomst fr.o.m.").ToString()), out fromSalaryRange)
				|| !int.TryParse((row.GetProperty("inkomst t.o.m.").ToString()), out toSalaryRange))
				{
					return -1;
				}
				if (fromSalaryRange <= monthlySalary && monthlySalary <= toSalaryRange)
				{
					if (!int.TryParse(row.GetProperty("kolumn 1").ToString(), out totalTaxReduction))
					{
						
						return -1;
					}
				}
			}
			return totalTaxReduction;
		}
		private async Task transformCSVToXMLIntermediate()
		{
			string kauID = "peremil100";
			string fileDateTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
			string logPath = @"Logs\INTERMEDIATE\";
			string logPathHR = @"Logs\HR\";

			string filePath = @"Data\updatedMaster.csv";

			string logFilePathAndName = logPath + "intermediate_" + kauID + '_' + fileDateTime + ".xml";
			string logFilePathAndNameHR = logPathHR + "HR_" + kauID + '_' + fileDateTime + ".csv";

			File.WriteAllText(logFilePathAndNameHR, File.ReadAllText(filePath));


			string[] source = File.ReadAllText(filePath).Split('\n');

			XElement employees = new XElement("Employees"); //"Root"-elementet

			for (int i = 0; i < source.Length; i++)
			{
				string[] fields = source[i].Split(',');
				XElement employee = new XElement("Employee"); //barn till "Root"-elementet ("Employees)
				employee.Add(new XElement("EmpID", fields[0]));
				employee.Add(new XElement("PersonID", fields[1]));
				employee.Add(new XElement("Firstname", fields[2]));
				employee.Add(new XElement("Middlename", fields[3]));
				employee.Add(new XElement("Lastname", fields[4]));
				employee.Add(new XElement("EmpType", fields[5]));
				employee.Add(new XElement("EmpExtent", fields[6]));
				employee.Add(new XElement("Designation", fields[7]));
				employee.Add(new XElement("Birthdate", fields[8]));
				employee.Add(new XElement("StreetAddress", fields[9]));
				employee.Add(new XElement("Areacode", fields[10]));
				employee.Add(new XElement("City", fields[11]));
				employee.Add(new XElement("Phone", fields[12]));
				employee.Add(new XElement("Email", fields[13]));
				employee.Add(new XElement("PrivateEmail", fields[14]));
				employee.Add(new XElement("Joindate", fields[15]));
				employee.Add(new XElement("Department", fields[16]));
				employee.Add(new XElement("Bank", fields[17]));
				employee.Add(new XElement("Account_no", fields[18]));
				employee.Add(new XElement("AnnualSalary", fields[19]));
				employee.Add(new XElement("Vacation_days", fields[20]));
				employee.Add(new XElement("DriverLicense", fields[21]));
				employee.Add(new XElement("DriverLicenseType", fields[20]));
				employees.Add(employee);
			}
			employees.Save(@"Data\intermediate.xml");
			employees.Save(logFilePathAndName);
		}
		private void transform_XMLToJson()
		{
			//TODO
			string kauID = "peremil100";
			string fileDateTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
			string logPath = @"Logs\TIME\";
			string logFilePathAndName = logPath + "time_" + kauID + '_' + fileDateTime + ".json";

			XmlDocument doc = new XmlDocument();
			string pathAndFileName = @"Data\time.xml";
			string jsonPathAndFileName = @"Data\time.json";
			doc.Load(pathAndFileName);
			foreach (XmlNode node in doc)
			{
				if (node.NodeType == XmlNodeType.XmlDeclaration)
				{
					doc.RemoveChild(node); //ta bort xml-deklarationen (första raden) innan konvertering
				}
			}
			string json = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);
			File.WriteAllText(jsonPathAndFileName, json);
			File.WriteAllText(logFilePathAndName, json);
		}
		public async Task ExportToQueue()
		{
			try
			{
				//LAB3: ändra filsökväg vid behov:
				string fileName = @"Data\salary.xml";
				if (File.Exists(fileName))
				{
					//LAB3: skicka meddelande till din kö för Salary
					await _producer.PublishToQueue(fileName, "SALARY");
					
				}
				else
				{
					//rtbOutput.Text = ("Export error. File: " + fileName + " is not found.");
				}
				
				fileName = @"Data\time.json";
				if (File.Exists(fileName))
				{
					//LAB3: skicka meddelande till din kö för Salary,
					await _producer.PublishToQueue(fileName, "TIME");
				}
				else
				{
					//MessageBox.Show("Export error. File: " + fileName + " is not found.");
				}
			}
			catch (Exception ex)
			{
				//MessageBox.Show("Error: " + ex.Message);
			}
		}
		public  async Task automaticTransformation()
		{
			await transformCSVToXMLIntermediate();
			await transformIntermediateToXML_Salary();
			await transformIntermediateToXML_Time();
		}

	}
}
