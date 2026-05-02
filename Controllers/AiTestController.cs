using System.Threading.Tasks;
using System.Web.Mvc;

public class AiTestController : Controller
{
    // استدعاء الخدمة التي أنشأتها في الخطوة السابقة
    private readonly ZhipuAiService _aiService = new ZhipuAiService();

    // رابط التجربة: /AiTest/Index
    public async Task<ActionResult> Index()
    {
        // نرسل سؤالاً بسيطاً للموديل للتأكد من الاتصال
        string testPrompt = "رد عليّ بجملة واحدة: هل تم الاتصال بنجاح مع مشروع الباشمهندس وائل؟";

        var result = await _aiService.CallAiAsync(testPrompt);

        // سنعرض النتيجة كنص خام (Plain Text) لسرعة التجربة
        return Content(result);
    }
}