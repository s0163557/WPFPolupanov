using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Unicode;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPFPolupanov.Models;
using Ydb.Sdk;
using Ydb.Sdk.Services.Query;
using Ydb.Sdk.Services.Table;
using Ydb.Sdk.Value;
using static Ydb.Sdk.Value.ResultSet;
using Microsoft.Office.Interop.Excel;

namespace WPFPolupanov
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private Driver driver;
        ObservableCollection<Models.Series> seriesList = new ObservableCollection<Models.Series>();
        private async Task<ObservableCollection<Models.Series>> MakeRequest(string request)
        {
            using var tableClient = new TableClient(driver, new TableClientConfig());
            ObservableCollection<Models.Series> series = new ObservableCollection<Models.Series>();

            var response = await tableClient.SessionExec(async session =>
            {
                var query = request;

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit()
                ).ConfigureAwait(false);
            }).ConfigureAwait(false);

            response.Status.EnsureSuccess();

            var queryResponse = (ExecuteDataQueryResponse)response;
            var resultSets = queryResponse.Result.ResultSets;

            foreach (var result in resultSets)
                foreach (Row currentRow in result.Rows)
                    series.Add(
                        new Models.Series(
                            (ulong)currentRow["series_id"],
                            (string?)currentRow["title"],
                            (string?)currentRow["series_info"],
                            (DateTime?)currentRow["release_date"]));
            seriesList = series;
            return seriesList;
        }

        public async void QueryExecute(object sender, RoutedEventArgs e)
        {
            await MakeRequest(QueryText.Text);
            QueryGrid.ItemsSource = seriesList;
        }

        public async void DriverInitialize()
        {
            var endpoint = "grpcs://ydb.serverless.yandexcloud.net:2135/";
            var database = "/ru-central1/b1gun4sltiom2uuk8j7u/etngvhoi95vjrii6c5ab";
            var token = "t1.9euelZqNnpCKmJ7MxpqNm86KycvKlO3rnpWaypmXzZPPnZXKlomJycmQyovl8_d0B2RQ-e8UWTgz_N3z9zQ2YVD57xRZODP8zef1656VmseYnsbJysqXlcnOm5OcyJiV7_zF656VmseYnsbJysqXlcnOm5OcyJiV.Y1ld9wibA7jt3QJJjy3Qwx_qIrbxGvR71oiqgHMU3Ubt8burWRYj6pN3DrMfl-zGWnP4E7c_vOLomCtWBICcDw";
            Ydb.Sdk.Auth.TokenProvider credentials = new Ydb.Sdk.Auth.TokenProvider(token);

            var config = new DriverConfig(
                endpoint,
                database,
                credentials
            );

            driver = new Driver(
                config
            );

            await driver.Initialize();
        }

        public MainWindow()
        {
            InitializeComponent();
            DriverInitialize();
        }

        private async void SeriesRead_Click(object sender, RoutedEventArgs e)
        {
            await MakeRequest("Select * from series");
            SeriesGrid.ItemsSource = seriesList;
        }

        private async void Create(Models.Series series)
        {
            using var tableClient = new TableClient(driver, new TableClientConfig());
            var response = await tableClient.SessionExec(async session =>
            {
                var query = @"
                            DECLARE $id AS Uint64;
                            DECLARE $title AS Utf8;
                            DECLARE $info AS Utf8;
                            DECLARE $release_date AS Date;

                            UPSERT INTO series (series_id, title, series_info, release_date) VALUES
                                ($id, $title, $info, $release_date);
                            ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>
                        {
                { "$id", YdbValue.MakeUint64(series.series_id) },
                { "$title", YdbValue.MakeUtf8(series.title) },
                { "$info", YdbValue.MakeUtf8(series.series_info) },
                { "$release_date", YdbValue.MakeDate((DateTime)series.release_date) }
                        }
                ).ConfigureAwait(false);
            }).ConfigureAwait(false);

            response.Status.EnsureSuccess();
        }

        private async void SeriesCreate_Click(object sender, RoutedEventArgs e)
        {
            Models.Series series = new Models.Series(UInt64.Parse(SeriesIDTB.Text), TitleTB.Text, SeriesInfoTB.Text, DateTime.UtcNow);
            Create(series);
        }

        private async void Delete(ulong id)
        {
            using var tableClient = new TableClient(driver, new TableClientConfig());
            var response = await tableClient.SessionExec(async session =>
            {
                var query = @"
                            DECLARE $id AS Uint64;

                            DELETE FROM series WHERE series_id=$id
                            ";

                return await session.ExecuteDataQuery(
                    query: query,
                    txControl: TxControl.BeginSerializableRW().Commit(),
                    parameters: new Dictionary<string, YdbValue>
                        {
                { "$id", YdbValue.MakeUint64(id) }
                        }
                ).ConfigureAwait(false);
            }).ConfigureAwait(false);

            response.Status.EnsureSuccess();
        }

        private async void SeriesDelete_Click(object sender, RoutedEventArgs e)
        {
            Delete(ulong.Parse(SeriesIDDelete.Text));
        }

        private async void SeriesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataGrid dg = sender as DataGrid;
            if (dg != null)
            {
                SeriesUpdateGrid.Visibility = Visibility.Visible;
                var cellInfo = SeriesGrid.SelectedCells[0];
                Models.Series value = cellInfo.Item as Models.Series;
                SeriesIDTBUpdate.Text = value.series_id.ToString();
                SeriesInfoTBUpdate.Text = value.series_info;
                TitleTBUpdate.Text = value.title;
                dg.UnselectAll();
            }
        }

        private void SeriesUpdate_Click(object sender, RoutedEventArgs e)
        {
            Delete(ulong.Parse(SeriesIDTBUpdate.Text));
            Models.Series series = new Models.Series(UInt64.Parse(SeriesIDTBUpdate.Text), TitleTBUpdate.Text, SeriesInfoTBUpdate.Text, DateTime.UtcNow);
            Create(series);
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Office.Interop.Excel._Application app = new Microsoft.Office.Interop.Excel.Application();
            Microsoft.Office.Interop.Excel._Workbook workbook = app.Workbooks.Add(Type.Missing);
            Microsoft.Office.Interop.Excel._Worksheet worksheet = null;
            worksheet = (_Worksheet?)workbook.Sheets["Sheet1"];
            worksheet = (_Worksheet?)workbook.ActiveSheet;
            worksheet.Name = "Exported from grid";

            for (int i = 1; i < SeriesGrid.Columns.Count + 1; i++)
                worksheet.Cells[1, i] = SeriesGrid.Columns[i].Header;
            

            for (int i = 0; i < SeriesGrid.Items.Count - 1; i++)
            {
                for (int j = 0; j < SeriesGrid.Columns.Count; j++)
                {
                    worksheet.Cells[i + 2, j + 1] = SeriesGrid.Items[i];
                }
            }
            workbook.SaveAs("c:\\output.xls", Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlExclusive, Type.Missing, Type.Missing, Type.Missing, Type.Missing);

        }
    }
}