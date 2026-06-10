using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TMDT.Utilities
{
    public class AiService
    {
        // Thay YOUR_API_KEY bằng API Key lấy từ Google AI Studio
        private const string ApiKey = "YOUR_API_KEY";
        private const string ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

        public async Task<string> GenerateReplyAsync(string chatHistory)
        {
            if (ApiKey == "YOUR_API_KEY")
            {
                return "Vui lòng nhập API Key của bạn vào file AiService.cs để tính năng này hoạt động!";
            }

            string systemPrompt = @"Bạn là trợ lý AI cho Admin của sàn thương mại điện tử Volox. 
Bạn đang giúp Admin trả lời tin nhắn của cửa hàng (Shop) hoặc người mua. 
Hãy đọc đoạn hội thoại sau và đưa ra một câu trả lời ngắn gọn, lịch sự, chuyên nghiệp, đúng trọng tâm nhất.
Chỉ trả về nội dung câu trả lời, không cần thêm giải thích hay lời chào dư thừa.

Nội dung đoạn hội thoại:
";

            string fullPrompt = systemPrompt + chatHistory;

            using (var client = new HttpClient())
            {
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = fullPrompt }
                            }
                        }
                    }
                };

                string jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync($"{ApiUrl}?key={ApiKey}", content);
                    response.EnsureSuccessStatusCode();

                    string responseString = await response.Content.ReadAsStringAsync();
                    var responseObject = JObject.Parse(responseString);

                    // Lấy đoạn text trả về từ Gemini
                    var generatedText = responseObject["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                    
                    return generatedText?.Trim() ?? "Xin lỗi, AI không thể tạo câu trả lời lúc này.";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("AI Error: " + ex.Message);
                    return "Xin lỗi, có lỗi xảy ra khi kết nối với máy chủ AI.";
                }
            }
        }

        public async Task<string> AnalyzeProductAsync(string productName, string description, decimal price)
        {
            if (ApiKey == "YOUR_API_KEY")
            {
                return "Vui lòng nhập API Key để sử dụng AI.";
            }

            string systemPrompt = @"Bạn là AI Kiểm duyệt Sản phẩm của sàn thương mại điện tử Volox.
Nhiệm vụ của bạn là đọc Thông tin Sản phẩm (Tên, Mô tả, Giá) và đánh giá xem sản phẩm này có vi phạm chính sách không.
Các vi phạm bao gồm: Vũ khí, ma túy, chất cấm, hàng giả/nhái (Fake, Rep 1:1), nội dung đồi trụy, từ ngữ thô tục, hoặc thông tin lừa đảo.
Hãy trả về ĐÚNG MỘT TRONG HAI KẾT QUẢ SAU (không dài dòng):
1. '✅ HỢP LỆ: Sản phẩm có vẻ an toàn.' (Nếu không thấy dấu hiệu vi phạm)
2. '⚠️ CẢNH BÁO: [Lý do ngắn gọn] Đề xuất: TỪ CHỐI.' (Nếu thấy có dấu hiệu vi phạm)

Thông tin sản phẩm cần duyệt:
";

            string productInfo = $"- Tên sản phẩm: {productName}\n- Giá: {price:N0} VNĐ\n- Mô tả: {description}";
            string fullPrompt = systemPrompt + productInfo;

            using (var client = new HttpClient())
            {
                var requestBody = new
                {
                    contents = new[] { new { parts = new[] { new { text = fullPrompt } } } }
                };

                string jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync($"{ApiUrl}?key={ApiKey}", content);
                    response.EnsureSuccessStatusCode();

                    string responseString = await response.Content.ReadAsStringAsync();
                    var responseObject = JObject.Parse(responseString);
                    var generatedText = responseObject["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                    
                    return generatedText?.Trim() ?? "Không thể phân tích.";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("AI Analyze Error: " + ex.Message);
                    return "Lỗi kết nối AI.";
                }
            }
        }

        public async Task<string> AnalyzeDashboardAsync(
            decimal monthlyRevenue, 
            decimal commissionsEarned, 
            int totalShops, 
            int pendingShops, 
            int totalProducts, 
            int pendingProducts,
            int totalUsers)
        {
            if (ApiKey == "YOUR_API_KEY")
            {
                return "Vui lòng nhập API Key để sử dụng AI.";
            }

            string systemPrompt = @"Bạn là Giám đốc Phân tích Kinh doanh (Data Analyst) của sàn thương mại điện tử Volox.
Nhiệm vụ của bạn là xem xét các chỉ số kinh doanh hiện tại và viết một đoạn báo cáo cực kỳ ngắn gọn (khoảng 3-4 câu).
Trong báo cáo cần:
1. Nhận xét về tình hình doanh thu và hoa hồng.
2. Nêu bật các điểm nghẽn hoặc công việc Admin cần ưu tiên xử lý ngay (dựa trên số lượng chờ duyệt).
3. Đưa ra 1 lời khuyên hoặc chiến lược thực tế.

Giọng điệu: Chuyên nghiệp, trực diện, không chào hỏi rườm rà.

Số liệu hiện tại của hệ thống:
";

            string dataInfo = $"- Doanh thu tháng: {monthlyRevenue:N0} VNĐ\n" +
                              $"- Hoa hồng phí sàn thu được: {commissionsEarned:N0} VNĐ\n" +
                              $"- Tổng người dùng: {totalUsers:N0}\n" +
                              $"- Tổng số cửa hàng đang hoạt động: {totalShops:N0}\n" +
                              $"- Số cửa hàng mới ĐANG CHỜ DUYỆT: {pendingShops:N0}\n" +
                              $"- Tổng sản phẩm trên sàn: {totalProducts:N0}\n" +
                              $"- Số sản phẩm ĐANG CHỜ DUYỆT: {pendingProducts:N0}";

            string fullPrompt = systemPrompt + dataInfo;

            using (var client = new HttpClient())
            {
                var requestBody = new
                {
                    contents = new[] { new { parts = new[] { new { text = fullPrompt } } } }
                };

                string jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync($"{ApiUrl}?key={ApiKey}", content);
                    response.EnsureSuccessStatusCode();

                    string responseString = await response.Content.ReadAsStringAsync();
                    var responseObject = JObject.Parse(responseString);
                    var generatedText = responseObject["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                    
                    return generatedText?.Trim() ?? "Không thể tạo báo cáo.";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("AI Dashboard Error: " + ex.Message);
                    return "Lỗi kết nối máy chủ AI khi tạo báo cáo.";
                }
            }
        }

        public async Task<string> SummarizeComplaintsAsync(System.Collections.Generic.List<string> complaints)
        {
            if (ApiKey == "YOUR_API_KEY")
            {
                return "Vui lòng nhập API Key để sử dụng AI.";
            }

            if (complaints == null || complaints.Count == 0)
            {
                return "Không có khiếu nại nào để phân tích.";
            }

            string systemPrompt = @"Bạn là Trợ lý Chăm sóc Khách hàng của sàn thương mại điện tử Volox.
Nhiệm vụ của bạn là đọc một danh sách các khiếu nại của khách hàng và TÓM TẮT lại những vấn đề NỔI CỘM nhất.
Yêu cầu:
1. Độ dài tối đa 2-3 câu.
2. Nêu bật xu hướng: Đa số khách hàng phàn nàn về vấn đề gì? (ví dụ: giao hàng chậm, hàng giả, thái độ shop).
3. Giọng điệu chuyên nghiệp, khách quan, báo cáo trực tiếp vấn đề.

Danh sách các nội dung khiếu nại:
";

            // Limit to max 30 complaints to avoid token limit and improve speed
            var limitedComplaints = complaints.Count > 30 ? complaints.GetRange(0, 30) : complaints;
            
            string complaintsText = "";
            for (int i = 0; i < limitedComplaints.Count; i++)
            {
                complaintsText += $"{i + 1}. {limitedComplaints[i]}\n";
            }

            string fullPrompt = systemPrompt + complaintsText;

            using (var client = new HttpClient())
            {
                var requestBody = new
                {
                    contents = new[] { new { parts = new[] { new { text = fullPrompt } } } }
                };

                string jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync($"{ApiUrl}?key={ApiKey}", content);
                    response.EnsureSuccessStatusCode();

                    string responseString = await response.Content.ReadAsStringAsync();
                    var responseObject = JObject.Parse(responseString);
                    var generatedText = responseObject["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                    
                    return generatedText?.Trim() ?? "Không thể tạo tóm tắt.";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("AI Complaint Error: " + ex.Message);
                    return "Lỗi kết nối máy chủ AI khi tạo tóm tắt khiếu nại.";
                }
            }
        }

        public async Task<string> GenerateMarketingContentAsync(string prompt)
        {
            if (ApiKey == "YOUR_API_KEY")
            {
                return "Vui lòng nhập API Key để sử dụng AI.";
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                return "Vui lòng nhập ý tưởng khuyến mãi để AI bắt đầu viết.";
            }

            string systemPrompt = @"Bạn là Chuyên gia Copywriter & Marketing thực chiến xuất sắc của sàn thương mại điện tử Volox.
Nhiệm vụ của bạn là viết nội dung (slogan, tiêu đề, mô tả) quảng cáo/khuyến mãi dựa trên ý tưởng của Admin.
Yêu cầu:
- Viết bằng Tiếng Việt, phong cách chốt sale đỉnh cao, đánh mạnh vào tâm lý FOMO (Sợ bỏ lỡ).
- LUÔN TRẢ VỀ CHÍNH XÁC 3 MẪU KHÁC NHAU (Option 1, Option 2, Option 3) tương ứng với 3 phong cách:
  + Mẫu 1: Giật gân, Khẩn cấp, Săn sale gấp gáp.
  + Mẫu 2: Đồng cảm, Kể chuyện, Thuyết phục nhẹ nhàng.
  + Mẫu 3: Ngắn gọn, Thẳng thắn, Đập thẳng vào lợi ích (Tiết kiệm bao nhiêu tiền).

Trình bày rõ ràng theo từng Option. Có sử dụng Emoji phù hợp.

Ý tưởng cần viết:
";

            string fullPrompt = systemPrompt + prompt;

            using (var client = new HttpClient())
            {
                var requestBody = new
                {
                    contents = new[] { new { parts = new[] { new { text = fullPrompt } } } }
                };

                string jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync($"{ApiUrl}?key={ApiKey}", content);
                    response.EnsureSuccessStatusCode();

                    string responseString = await response.Content.ReadAsStringAsync();
                    var responseObject = JObject.Parse(responseString);
                    var generatedText = responseObject["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                    
                    return generatedText?.Trim() ?? "Không thể tạo nội dung. Vui lòng thử lại.";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("AI Marketing Error: " + ex.Message);
                    return "Lỗi kết nối máy chủ AI khi tạo Content Marketing.";
                }
            }
        }
    }
}
