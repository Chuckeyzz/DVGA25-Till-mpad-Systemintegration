using Newtonsoft.Json;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using File = System.IO.File;

namespace DVGA25_Datatransformer
{
	public partial class Form1 : Form
	{
		private Producer producer;
		private Consumer consumer;

		public Form1()
		{
			InitializeComponent();
			producer = new Producer();
			consumer = new Consumer();

			//attach event listner to event in consumer
			consumer.MessageProcessed += Consumer_MessageProcessed;
		}
		
		private void btnImport_Click(object sender, EventArgs e)
		{
			try
			{
				importFromSFTP("employee_master.csv");
				//rtbInput.Text = File.ReadAllText(@"C:\Temp\intermediate.xml");
				//rtbInput.Text = File.ReadAllText(@"C:\Temp\Employee.csv");
			}
			catch (Exception ex)
			{
				rtbImportStatus.Text += "Error in import: Exception: " + ex.Message + "\n";
			}
		}

		private void btnExport_Click(object sender, EventArgs e)
		{
			string? fileName;
			string filePathAndName;

			fileName = saveDataToFile(); //spara undan data till fil inför export
			if (fileName != null)
			{
				//TODO LAB1: byt eventuellt filsökväg till filen som ska exporteras
				filePathAndName = @"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\Labbar\DVGA25_Datatransformer\Temp\" + fileName;

				exportFileToSFTP(filePathAndName, fileName);
			}
			else
			{
				rtbExportStatus.Text = "Error: export file could not be created";
			}
		}

		private async void btnCopy_Click(object sender, EventArgs e)
		{
			copyData();
		}

		private void copyData()
		{
			rtbOutput.Text = rtbInput.Text;
		}
		private string? saveDataToFile()
		{
			//spara data i "exportfönstret" till fil:
			string tempData = rtbOutput.Text;
			string[] textLines = tempData.Split("\n");

			//TODO LAB1: ändra till ditt kau-id:
			string kau_id = "peremil100";
			string fileDateTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");

			//TODO LAB1: byt eventuellt filsökväg till filen som ska exporteras 
			string destinationPathAndFileName = @"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\Labbar\DVGA25_Datatransformer\Temp\";
			string destinationFileName;


			try
			{
				destinationFileName = kau_id + "_" + fileDateTime + ".txt";
				destinationPathAndFileName += destinationFileName;

				rtbExportStatus.Text += "Destination filename: " + destinationFileName + "\n";
				StreamWriter sw = new StreamWriter(destinationPathAndFileName);
				rtbExportStatus.Text += "File Stream open. Writing " + textLines.Length.ToString() + " lines.\n";

				for (int i = 0; i < textLines.Length; i++)
				{
					sw.WriteLine(textLines[i]);
				}
				sw.Close();

				return destinationFileName;

			}
			catch (Exception e)
			{
				rtbExportStatus.Text += "Error in saveDataToFile: Exception: " + e.Message + "\n";
			}
			finally
			{
			}
			return null; //returnera null om filen inte kunde sparas
		}
		private void exportFileToSFTP(string sourceFileAndPath, string destinationFileName)
		{
			//TODO LAB1: peka ut din privata SSH-nyckel och byt ut till ditt kau-id 
			var privateKeyFile = new PrivateKeyFile(@"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\git_key");
			string kau_ID = "peremil100";

			using SftpClient client = new("vortex.cse.kau.se", 22, kau_ID, privateKeyFile);

			try
			{
				rtbExportStatus.Text += "Connecting...\n";
				client.Connect();
				if (client.IsConnected)
				{
					rtbExportStatus.Text += "Connected!\n";
					//Previous call to Upload files to \out in sftp server
					//client.UploadFile(File.OpenRead(@sourceFileAndPath), "out/" + destinationFileName);

					client.UploadFile
						(
							File.OpenRead
							(
								@"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\Labbar\DVGA25_Datatransformer\Temp\salary.xml"
							),
							"out/" +
							@"salary_peremil100_" +
							DateTime.Now.ToString("yyyyMMddHHmmssfff") +
							".xml"
						);
					client.UploadFile
						(File.OpenRead
							(
								@"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\Labbar\DVGA25_Datatransformer\Temp\time.xml"
							),
							"out/" +
							@"time_peremil100_" +
							DateTime.Now.ToString("yyyyMMddHHmmssfff") +
							".xml"
						);
					rtbExportStatus.Text += "File uploaded.\n";

					//kod för att lista innehållet i "out" mappen om man önskar:
					//foreach (var sftpFile in client.ListDirectory("out"))
					//{
					//   rtbExportStatus.Text += $"\t{sftpFile.FullName}\n";
					//}
					client.Disconnect();
					rtbExportStatus.Text += "Disconnected.\n";
				}
			}
			catch (Exception e) when (e is SshConnectionException || e is SocketException || e is ProxyException)
			{
				rtbExportStatus.Text += $"Error connecting to server: {e.Message}\n";
			}
			catch (SshAuthenticationException e)
			{
				rtbExportStatus.Text += $"Failed to authenticate: {e.Message}\n";
			}
			catch (SftpPermissionDeniedException e)
			{
				rtbExportStatus.Text += $"Operation denied by the server: {e.Message}\n";
			}
			catch (SshException e)
			{
				rtbExportStatus.Text += $"Sftp Error: {e.Message}\n";
			}

		}


		//change import from SFTP with Rabbit PUSH API 
		private void importFromSFTP(string filename)
		{
			string path = "in/" + filename;

			//TODO LAB1: använd dina inloggningsuppgifter
			var privateKeyFile = new PrivateKeyFile(@"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\git_key");
			string kau_ID = "peremil100";
			//----------------------------------------------------
			using SftpClient client = new("vortex.cse.kau.se", 22, kau_ID, privateKeyFile);

			try
			{
				rtbImportStatus.Text += "Trying to connect to sftp \n";

				client.Connect();
				if (client.IsConnected)
				{
					rtbImportStatus.Text += "Connected to sftp \n";
					rtbImportStatus.Text += "Reading file " + path + "\n";
					rtbInput.Text = client.ReadAllText(path);
					rtbImportStatus.Text += "File read ok \n";
					client.Disconnect();
					rtbImportStatus.Text += "Disconnected \n";
				}
			}
			catch (Exception e) when (e is SshConnectionException || e is SocketException || e is ProxyException)
			{
				rtbImportStatus.Text += $"Error connecting to server: {e.Message}\n";

			}
			catch (SshAuthenticationException e)
			{
				rtbImportStatus.Text += $"Failed to authenticate: {e.Message}\n";
			}
			catch (SftpPermissionDeniedException e)
			{
				rtbImportStatus.Text += $"Operation denied by the server: {e.Message}\n";
			}
			catch (SshException e)
			{
				rtbImportStatus.Text += $"Sftp Error: {e.Message}\n";
			}
		}

		private async void btnTransform_Click(object sender, EventArgs e)
		{

			await transformCSVToXMLIntermediate();

			//XML_Time calls XMLtoJson aswell 
			//Path hardcoded in transform_XMLToJson to only touch Time.xml
			await transformIntermediateToXML_Time();
			await transformIntermediateToXML_Salary();

		}
		private async Task transformIntermediateToXML_Time()
		{
			string kauID = "peremil100";
			string fileDateTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
			string logPath = @"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\Labbar\DVGA25_Datatransformer\LOGFILES\TIME\";
			string logFilePathAndName = logPath + "time_" + kauID + '_' + fileDateTime + ".xml";
			try
			{
				XmlDocument doc = new();
				doc.Load(@"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\Labbar\DVGA25_Datatransformer\Temp\intermediate.xml");
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
				string pathAndFileName = @"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\Labbar\DVGA25_Datatransformer\Temp\time.xml";
				employees.Save(pathAndFileName);
				employees.Save(logFilePathAndName);
				transform_XMLToJson();
			}
			catch (Exception e)
			{
				rtbExportStatus.Text += "Error in formIntermediateToXML_App: Exception: " + e.Message + "\n";
			}
		}

		private async Task transformIntermediateToXML_Salary()
		{
			string kauID = "peremil100";
			string fileDateTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
			string logPath = @"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\Labbar\DVGA25_Datatransformer\LOGFILES\SALARY\";
			string logFilePathAndName = logPath + "salary_" + kauID + '_' + fileDateTime + ".xml";
			try
			{
				XmlDocument doc = new();
				doc.Load(@"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\Labbar\DVGA25_Datatransformer\Temp\intermediate.xml");
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
							rtbOutput.Text += "Error Converting salary to integer value!";
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
				string pathAndFileName = @"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\Labbar\DVGA25_Datatransformer\Temp\salary.xml";
				
				employees.Save(pathAndFileName);
				employees.Save(logFilePathAndName);
				rtbOutput.Text += File.ReadAllText(pathAndFileName);

			}
			catch (Exception e)
			{
				rtbExportStatus.Text += "Error in formIntermediateToXML_App: Exception: " + e.Message + "\n";
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
					rtbOutput.Text = "Error Salary range invalid";
				}
				if (fromSalaryRange <= monthlySalary && monthlySalary <= toSalaryRange)
				{
					if (!int.TryParse(row.GetProperty("kolumn 1").ToString(), out totalTaxReduction))
					{
						rtbOutput.Text = "Error retrieving tax reduction";
						return -1;
					}
				}
			}
			return totalTaxReduction;
		}

		private void transformFlatfileToXML_App()
		{
			//string rtbText = rtbInput.Text;
			//string[] source = rtbText.Split('\n'); //dela upp varje rad i flatfilen


			string[] source = File.ReadAllLines(@"C:\Temp\Employee.txt");

			XElement employees = new XElement("Employees"); //"Root"-elementet

			for (int i = 0; i < source.Length; i++)
			{
				XElement employee = new XElement("Employee"); //barn till "Root"-elementet ("Employees)
				employee.Add(new XElement("EmpID", source[i].Substring(0, 3)));
				employee.Add(new XElement("Firstname", source[i].Substring(3, 10).TrimEnd()));
				employee.Add(new XElement("Lastname", source[i].Substring(13, 12).TrimEnd()));
				employee.Add(new XElement("Birthdate", source[i].Substring(25, 6)));
				employee.Add(new XElement("Address", source[i].Substring(31, 15).TrimEnd()));
				employee.Add(new XElement("City", source[i].Substring(46, 10).TrimEnd()));
				employee.Add(new XElement("Phone", source[i].Substring(56, 8).TrimEnd()));
				employee.Add(new XElement("Email", source[i].Substring(64, 30).TrimEnd()));
				employees.Add(employee);
			}
			employees.Save(@"C:\Temp\intermediate.xml");


			rtbOutput.Text = employees.ToString();

		}

		private void transformCSVToXML_App()
		{
			string rtbText = rtbInput.Text;

			string[] source = rtbText.Split('\n');
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
			rtbOutput.Text = employees.ToString();

		}
		private async Task transformCSVToXMLIntermediate()
		{
			string kauID = "peremil100";
			string fileDateTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
			string logPath = @"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\Labbar\DVGA25_Datatransformer\LOGFILES\INTERMEDIATE\";
			string logPathHR = @"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\Labbar\DVGA25_Datatransformer\LOGFILES\HR\";

			string filePath = @"C:\Temp\updatedMaster.csv";

			string logFilePathAndName = logPath + "intermediate_" + kauID + '_' + fileDateTime + ".xml";
			string logFilePathAndNameHR = logPathHR + "HR_" + kauID + '_' + fileDateTime + ".csv";

			File.WriteAllText(logFilePathAndNameHR, File.ReadAllText(filePath));


			string[] source = rtbInput.Text.Split('\n');

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
			employees.Save(@"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\Labbar\DVGA25_Datatransformer\Temp\intermediate.xml");
			employees.Save(logFilePathAndName);
		}

		private XmlDocument JsonToXML(string json)
		{
			XmlDocument doc = new XmlDocument();

			using (var reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json), XmlDictionaryReaderQuotas.Max))
			{
				XElement xml = XElement.Load(reader);
				doc.LoadXml(xml.ToString());
			}

			return doc;
		}
		private void transform_XMLToJson()
		{
			//TODO
			string kauID = "peremil100";
			string fileDateTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
			string logPath = @"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\Labbar\DVGA25_Datatransformer\LOGFILES\TIME\";
			string logFilePathAndName = logPath + "time_" + kauID + '_' + fileDateTime + ".json";

			XmlDocument doc = new XmlDocument();
			string pathAndFileName = @"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\Labbar\DVGA25_Datatransformer\Temp\time.xml";
			string jsonPathAndFileName = @"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\Labbar\DVGA25_Datatransformer\Temp\time.json";
			doc.Load(pathAndFileName);
			foreach (XmlNode node in doc)
			{
				if (node.NodeType == XmlNodeType.XmlDeclaration)
				{
					doc.RemoveChild(node); //ta bort xml-deklarationen (första raden) innan konvertering
				}
			}
			string json = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);
			rtbOutput.Text += json;
			File.WriteAllText(jsonPathAndFileName, json);
			File.WriteAllText(logFilePathAndName, json);
		}

		private async void btnexportToQueue_Click(object sender, EventArgs e)
		{
			try
			{
				//LAB3: ändra filsökväg vid behov:
				string fileName = @"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\Labbar\DVGA25_Datatransformer\Temp\salary.xml";
				if (File.Exists(fileName))
				{
					//LAB3: skicka meddelande till din kö för Salary
					await producer.PublishToQueue(fileName, "peremil100_SALARY", "SALARY");
					rtbExportStatus.Text += "Salary Exported to Queue \n";
					//consumer.Receive();
				}
				else
				{
					rtbOutput.Text = ("Export error. File: " + fileName + " is not found.");
				}
				//LAB3: ändra filsökväg vid behov:
				fileName = @"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\Labbar\DVGA25_Datatransformer\Temp\time.json";
				if (File.Exists(fileName))
				{
					//LAB3: skicka meddelande till din kö för Salary,
					await producer.PublishToQueue(fileName, "peremil100_TIME", "TIME");
					rtbExportStatus.Text += "Time Exported to Queue \n";
				}
				else
				{
					MessageBox.Show("Export error. File: " + fileName + " is not found.");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error: " + ex.Message);
			}
		}

		private async void importFromQueuebtn_Click(object sender, EventArgs e)
		{
			string kauID = "peremil100";
			string fileDateTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
			string logPath = @"C:\Plugg\Högskoleingenjör i Datateknik\DVGA25 - Tillämpad systemintegration\Labbar\DVGA25_Datatransformer\LOGFILES\HR\";
			string logFilePathAndName = logPath + "hr_" + kauID + '_' + fileDateTime + ".csv";

			await consumer.ReceiveAsync();
			string filePath = @"C:\Temp\updatedMaster.csv";

			File.WriteAllText(logFilePathAndName, File.ReadAllText(filePath));

			if (File.Exists(filePath))
			{
				rtbInput.Text = File.ReadAllText(filePath);
				rtbImportStatus.Text += "Update recieved from from queue \n";
			}
			else
			{
				rtbImportStatus.Text += "Unable to recieve update from from queue \n";
			}
		}
		//start async connection to queue
		private async void Form1_Load(object sender, EventArgs e)
		{
			await consumer.ReceiveAsync();
		}

		private async void Consumer_MessageProcessed()
		{
			//await message processed to updatedMaster.CSV from consumer
			if (InvokeRequired)
			{
				Invoke(new Action(Consumer_MessageProcessed));
				return;
			}
			//when message from HR is processed try following
			try
			{
				rtbInput.Text = File.ReadAllText(@"C:\Temp\updatedMaster.csv");
				await automaticTransformation();
				//queue pops on Salary but not Time for some reason.
				btnexportToQueue_Click(this, null);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Consumer_MessageProcessed error: " + ex.Message);
			}
		}
		private async Task automaticTransformation()
		{
			await transformCSVToXMLIntermediate();
			await transformIntermediateToXML_Salary();
			await transformIntermediateToXML_Time();
		}

	}

}
