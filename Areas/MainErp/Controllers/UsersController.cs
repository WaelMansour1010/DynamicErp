using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Common.Users;

namespace MyERP.Areas.MainErp.Controllers
{
    public class UsersController : MainErpControllerBase
    {
        private readonly MainErpDbConnectionFactory _connectionFactory;
        private readonly SharedUsersRepository _repository;

        public UsersController()
            : this(new MainErpDbConnectionFactory())
        {
        }

        public UsersController(MainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _repository = new SharedUsersRepository(() => _connectionFactory.CreateOpenConnection());
        }

        public ActionResult Index(string searchText = "", int page = 1, int pageSize = 30)
        {
            ViewBag.ActiveScreen = "users";
            ViewBag.SearchText = searchText ?? string.Empty;
            var safePage = System.Math.Max(1, page);
            var safePageSize = System.Math.Max(10, System.Math.Min(pageSize, 100));
            ViewBag.Page = safePage;
            ViewBag.PageSize = safePageSize;

            var result = _repository.Search(searchText, safePage, safePageSize);
            ViewBag.TotalRows = result.TotalRows;
            ViewBag.DatabaseName = CurrentDatabaseName();
            ViewBag.CanEditUsers = MainErpUserContext != null && MainErpUserContext.IsAdmin;
            return View(result.Items);
        }

        private string CurrentDatabaseName()
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                return connection.Database;
            }
        }
    }
}
