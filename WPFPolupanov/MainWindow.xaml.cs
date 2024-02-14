using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Unicode;
using System.Windows;
using System.Windows.Controls;
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
using static Ydb.Sdk.Value.ResultSet;

namespace WPFPolupanov
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Driver driver;
        ObservableCollection<Series> seriesList = new ObservableCollection<Series>();
        private async Task<ObservableCollection<Series>> MakeRequest(string request)
        {
            using var tableClient = new TableClient(driver, new TableClientConfig());
            ObservableCollection<Series> series = new ObservableCollection<Series>();

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
                        new Series(
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
            var token = "t1.9euelZqcmJnOzMqKjc_KmZWJxpqQiu3rnpWaypmXzZPPnZXKlomJycmQyovl8_dMRk1R-e9HZiMd_t3z9wx1SlH570dmIx3-zef1656VmpLPx8ePks2el5aQm5qVi5SK7_zF656VmpLPx8ePks2el5aQm5qVi5SK.XO3r22NwrM4wEGzd_7BxudamdohRK1sl0EjeOrMWyATV3QRIbqLzUZnvcc3TJGup-E4kMuhocmYKX_GhlppoAw";
            Ydb.Sdk.Auth.TokenProvider credentials = new Ydb.Sdk.Auth.TokenProvider(token);

            var config = new DriverConfig(
                endpoint,
                database,
                credentials
            );

            driver = new Driver(
                config
            );

            await driver.Initialize().ConfigureAwait(false);
        }

      

        public MainWindow()
        {
            InitializeComponent();
            DriverInitialize();
            QueryGrid.ItemsSource = seriesList;
        }
    }
}