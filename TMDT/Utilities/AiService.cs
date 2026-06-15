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
        private string ApiKey
        {
            get
            {
                try
                {
                    string keyPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "AiKey.txt");
                    if (System.IO.File.Exists(keyPath))
                    {
                        var key = System.IO.File.ReadAllText(keyPath).Trim();
                        if (!string.IsNullOrEmpty(key) && key != "YOUR_API_KEY") 
                            return key;
                    }
                }
                catch { }
                return "YOUR_API_KEY";
            }
        }
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

        public async Task<string> AnalyzeProductWithImageAsync(string productName, string description, decimal price, string base64Image)
        {
            if (ApiKey == "YOUR_API_KEY")
            {
                return "Vui lòng nhập API Key để sử dụng AI.";
            }

            string systemPrompt = @"Bạn là AI Kiểm duyệt Sản phẩm đa phương tiện (Multi-modal) của sàn thương mại điện tử Volox.
Nhiệm vụ của bạn là đọc Thông tin Sản phẩm (Tên, Mô tả, Giá) và QUAN SÁT HÌNH ẢNH đính kèm để đánh giá xem sản phẩm này có vi phạm chính sách không.
Các vi phạm bao gồm: Vũ khí, ma túy, chất cấm, hàng giả/nhái (chứa logo Gucci, Chanel, Nike... fake), nội dung đồi trụy, thô tục.
Đặc biệt chú ý xem hình ảnh có chứa yếu tố cấm hoặc không khớp với mô tả hay không.
Hãy trả về ĐÚNG MỘT TRONG HAI KẾT QUẢ SAU (không dài dòng):
1. '✅ HỢP LỆ (Đã soi ảnh): Sản phẩm và hình ảnh an toàn.' (Nếu không có vi phạm)
2. '⚠️ CẢNH BÁO: [Lý do ngắn gọn phát hiện từ ảnh/text] Đề xuất: TỪ CHỐI.' (Nếu thấy có dấu hiệu vi phạm)

Thông tin sản phẩm cần duyệt:
";

            string productInfo = $"- Tên sản phẩm: {productName}\n- Giá: {price:N0} VNĐ\n- Mô tả: {description}";
            string fullPrompt = systemPrompt + productInfo;

            // Xử lý base64 (cắt bỏ phần prefix data:image/jpeg;base64, nếu có)
            string cleanBase64 = base64Image.Contains(",") ? base64Image.Split(',')[1] : base64Image;

            using (var client = new HttpClient())
            {
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = fullPrompt },
                                new
                                {
                                    inline_data = new
                                    {
                                        mime_type = "image/jpeg",
                                        data = cleanBase64
                                    }
                                }
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
                    var generatedText = responseObject["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                    
                    return generatedText?.Trim() ?? "Không thể phân tích hình ảnh.";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("AI Error (Vision): " + ex.Message);
                    return "Lỗi kết nối AI khi phân tích ảnh. " + ex.Message;
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

        public async Task<string> ScanFraudProductsAsync(List<string> productInfos)
        {
            if (ApiKey == "YOUR_API_KEY")
            {
                return "Vui lòng nhập API Key để sử dụng AI.";
            }

            if (productInfos == null || productInfos.Count == 0)
            {
                return "Không có sản phẩm nào để quét.";
            }

            string systemPrompt = @"Bạn là Chuyên gia Kiểm duyệt An ninh mạng của Sàn thương mại điện tử Volox.
Nhiệm vụ của bạn là quét danh sách Tên sản phẩm và Giá bán để phát hiện dấu hiệu GIAN LẬN hoặc VI PHẠM:
1. Hàng cấm/Hàng nguy hiểm: Vũ khí (súng, dao găm, kiếm), chất cấm, pháo nổ, nội dung đồi trụy.
2. Hàng giả/Lừa đảo (Fake/Scam): Các sản phẩm thương hiệu lớn (iPhone, Samsung, Rolex, Macbook, PS5...) nhưng giá RẤT RẺ một cách phi lý (Ví dụ: iPhone 15 giá 2 triệu, Laptop Gaming giá 1 triệu).

Hãy phân tích kỹ từng sản phẩm trong danh sách và chỉ trả về Báo cáo Cảnh báo cho những sản phẩm vi phạm.
- Nếu CÓ vi phạm: Liệt kê rõ tên sản phẩm, lý do nghi ngờ.
- Nếu KHÔNG có sản phẩm nào vi phạm: Trả về chính xác câu: ""Không phát hiện sản phẩm gian lận hoặc vi phạm nào trong danh sách.""

Danh sách sản phẩm (Định dạng: Tên sản phẩm | Giá tiền):
";

            string joinedProducts = string.Join("\n", productInfos);
            string fullPrompt = systemPrompt + joinedProducts;

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
                    
                    return generatedText?.Trim() ?? "Không thể phân tích. Vui lòng thử lại.";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("AI Fraud Scan Error: " + ex.Message);
                    return "Lỗi kết nối máy chủ AI khi quét gian lận.";
                }
            }
        }

        public async Task<int> SuggestCategoryAsync(string productName, string description, Dictionary<int, string> categories)
        {
            if (ApiKey == "YOUR_API_KEY" || categories == null || categories.Count == 0)
            {
                return -1;
            }

            string categoryListStr = string.Join("\n", categories.Select(kv => $"{kv.Key}: {kv.Value}"));

            string systemPrompt = @"Bạn là AI Thủ Thư của Sàn thương mại điện tử Volox.
Nhiệm vụ của bạn là đọc Tên và Mô tả sản phẩm, sau đó đối chiếu với Danh sách Danh mục hiện có trong hệ thống và chọn ra 1 Danh mục phù hợp nhất.
BẠN CHỈ ĐƯỢC PHÉP TRẢ VỀ DUY NHẤT MỘT CON SỐ (ID CỦA DANH MỤC), KHÔNG ĐƯỢC GIẢI THÍCH HAY VIẾT THÊM BẤT KỲ CHỮ NÀO KHÁC.

Danh sách các Danh mục hợp lệ (Định dạng: ID: Tên danh mục):
" + categoryListStr + @"

Thông tin sản phẩm:
- Tên sản phẩm: " + productName + @"
- Mô tả: " + description + @"

Trả về ID (số nguyên) của danh mục chuẩn nhất:";

            using (var client = new HttpClient())
            {
                var requestBody = new
                {
                    contents = new[] { new { parts = new[] { new { text = systemPrompt } } } }
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
                    
                    if (int.TryParse(generatedText?.Trim(), out int categoryId))
                    {
                        if (categories.ContainsKey(categoryId))
                        {
                            return categoryId;
                        }
                    }
                    return -1;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("AI Category Error: " + ex.Message);
                    return -1;
                }
            }
        }

        public async Task<string> SuggestChatRepliesAsync(string customerMessage)
        {
            if (ApiKey == "YOUR_API_KEY")
            {
                return "Dạ, vâng ạ.|Chào bạn, sản phẩm này hiện còn hàng ạ.|Xin lỗi, hiện tại shop chưa thể hỗ trợ ngay.";
            }

            if (string.IsNullOrWhiteSpace(customerMessage))
            {
                return "Chào bạn, mình có thể giúp gì cho bạn?|Dạ, bạn cần tư vấn thêm về sản phẩm nào ạ?|Xin chào, cảm ơn bạn đã quan tâm đến shop!";
            }

            string systemPrompt = @"Bạn là trợ lý AI thông minh cho Chủ Cửa Hàng (Seller) trên sàn thương mại điện tử Volox.
Nhiệm vụ của bạn là đọc tin nhắn mới nhất của khách hàng và đề xuất 3 câu trả lời ngắn gọn, lịch sự, thân thiện và mang tính chốt sale để người bán có thể chọn nhanh.
Yêu cầu bắt buộc:
- LUÔN LUÔN trả về đúng 3 câu gợi ý.
- Các câu gợi ý phải được phân tách bằng ký tự '|' (Pipe). KHÔNG CÓ KÝ TỰ XUỐNG DÒNG, KHÔNG ĐÁNH SỐ THỨ TỰ.
- Ví dụ trả về chuẩn: Dạ còn ạ, bạn đặt hàng ngay nhé!|Xin chào, shop có thể giúp gì cho bạn?|Dạ sản phẩm này vừa hết hàng ạ.

Tin nhắn của khách hàng:
";

            string fullPrompt = systemPrompt + customerMessage;

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
                    
                    return generatedText?.Trim().Replace("\n", "").Replace("\r", "") ?? "Dạ vâng ạ.|Xin chào!|Shop xin lỗi ạ.";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("AI Chat Suggestion Error: " + ex.Message);
                    return "Chào bạn!|Dạ vâng ạ.|Cảm ơn bạn đã liên hệ.";
                }
            }
        }
    }
}
