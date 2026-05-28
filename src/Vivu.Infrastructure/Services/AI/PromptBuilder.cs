using System.Text;
using Vivu.Domain.AI;
using System.Text.Encodings.Web;
using System.Text.Json;
using Vivu.Application.DTOs.Responses.AI;
using Vivu.Application.Interfaces.AI;
using Vivu.Application.UseCases.AI.StreamGenerateTrip;
using Vivu.Domain.Entities;
using Vivu.Domain.Enums;
using static Vivu.Domain.Errors.DomainErrors;

namespace Vivu.Infrastructure.Services.AI
{
    public class PromptBuilder : IPromptBuilder
    {
        public string BuildTripPlanPrompt(TripPlanDto request, List<Domain.Entities.Location> availableLocations, TripConstraints? constraints = null, UserPersonalizationContext? personalization = null)
        {
            var locationsText = BuildGroupedLocationsText(availableLocations, request.Destination);

            var daysCount = (request.EndDate.ToDateTime(TimeOnly.MinValue) -
                            request.StartDate.ToDateTime(TimeOnly.MinValue)).Days;

            var preferencesText = request.Preferences.Any()
                ? $"- Preferences: {string.Join(", ", request.Preferences)}"
                : "";

            var personalizationText = BuildPersonalizationBlock(personalization);

            var budgetText = request.Budget.HasValue
                ? $"- Budget level: {request.Budget switch
                {
                    TripBudget.Budget => "Tiết kiệm (budget-friendly) - prioritize affordable options, local street food, public transport",
                    TripBudget.Standard => "Trung bình (mid-range) - mix of affordable and comfortable options",
                    TripBudget.Luxury => "Cao cấp (luxury) - prioritize premium experiences, fine dining, comfortable transport",
                    _ => ""
                }}"
                : "";

            var notesText = !string.IsNullOrWhiteSpace(request.Notes)
                ? $"\n            **Additional notes from user:**\n            {request.Notes}"
                : "";

            var titleHint = !string.IsNullOrWhiteSpace(request.Title)
                ? $"- Use this trip title: \"{request.Title}\""
                : "";

            var constraintsBlock = BuildConstraintsBlock(constraints);

            var dayStartTime = constraints?.StartTimeEachDay.HasValue == true
                ? constraints.StartTimeEachDay.Value.ToString("HH:mm")
                : "08:00";

            var dayEndTime = constraints?.EndTimeEachDay.HasValue == true
                ? constraints.EndTimeEachDay.Value.ToString("HH:mm")
                : "21:00";

            return $$$"""
            You are an expert travel planner. Create a detailed {{{daysCount}}}-day itinerary for {{{request.Destination}}}.

            **Available Locations (pre-selected and grouped by geographic area):**
            {{{locationsText}}}

            **Trip Information:**
            - Destination: {{{request.Destination}}}
            - Duration: {{{daysCount}}} days ({{{request.StartDate:yyyy-MM-dd}}} to {{{request.EndDate:yyyy-MM-dd}}})
            - Group size: {{{request.GroupSize}}} {{{(request.GroupSize == 1 ? "person" : "people")}}}
            {{{preferencesText}}}
            {{{budgetText}}}
            {{{titleHint}}}

            {{{personalizationText}}}

            **MANDATORY USER CONSTRAINTS — HIGHEST PRIORITY, OVERRIDES ALL OTHER RULES:**
            {{{constraintsBlock}}}

            **Requirements:**
            1. ACCOMMODATION RULE: You MUST choose EXACTLY ONE [HOTEL] for the ENTIRE trip. You MUST use the EXACT SAME [HOTEL] (same locationIndex) as the starting and ending point for EVERY SINGLE DAY. Do not change hotels.
            2. Select locations ONLY from the list above using their INDEX number.
            3. GEOGRAPHIC RULE: Assign EXACTLY ONE distinct Area to each day. Do NOT mix Areas in a single day, except [HOTEL].
            4. Food locations [FOOD] spread throughout: breakfast ~{{{dayStartTime}}}, lunch ~12:00, dinner ~18:30.
            5. Balance activities: mix of culture, nature, food, and relaxation.
            6. Match food and activity choices to Budget and Preferences.
            7. locationIndex MUST be a positive integer matching the INDEX shown in the list.
            8. ALTERNATIVES RULE: For EVERY primary location (except HOTEL), you MUST provide EXACTLY 2 alternative backup locations. 
                - DO NOT BE LAZY. You MUST search the available list to find 2 alternatives.
                - Alternatives MUST have the EXACT SAME category tag as the primary (e.g., if primary is [FOOD], alternatives MUST be [FOOD]; if [SHOP], alternatives MUST be [SHOP]).
                - Alternatives MUST be located in the SAME Area as the primary location.
                - Alternatives MUST be unique (different from the primary and from each other).

            **Output Format - STRICT JSON:**
            {
              "title": "Exciting {{{daysCount}}}-Day Trip to {{{request.Destination}}}",
              "description": "A brief 2-3 sentence overview of the trip",
              "start": "{{{request.StartDate:yyyy-MM-dd}}}",
              "end": "{{{request.EndDate:yyyy-MM-dd}}}",
              "size": {{{request.GroupSize}}},
              "days": [
                {
                  "dayIndex": 1,
                  "date": "{{{request.StartDate:yyyy-MM-dd}}}",
                  "title": "Day 1: Exploring...",
                  "locations": [
                    {
                      "locationIndex": 1,
                      "name": "EXACT name from list",
                      "description": "What to do here",
                      "startTime": "09:00",
                      "endTime": "11:30",
                      "transportMode": "walking",
                      "orderIndex": 1,
                      "alternatives": [
                        { "locationIndex": 5, "reason": "Cùng khu vực, phù hợp khi trời mưa" },
                        { "locationIndex": 9, "reason": "Lựa chọn thay thế gần đó" }
                      ]
                    }
                  ]
                }
              ]
            }

            **CRITICAL RULES:**
            - locationIndex MUST be a positive integer from the INDEX shown above (applies to BOTH primary and alternatives)
            - DO NOT use GUIDs or strings for locationIndex
            - DO NOT reuse the same locationIndex across primary locations AND their sibling alternatives within the same day
            - GLOBAL NO DUPLICATE RULE: A location (identified by its locationIndex) MUST NEVER appear more than once as a primary location in the entire itinerary (except the single chosen [HOTEL]). Once a location is visited, it cannot be visited again.
            - INTRA-DAY NO DUPLICATE: Primary locations and their alternatives MUST be mutually exclusive. Never put the primary locationIndex inside its own alternatives array.
            - MANDATORY LOCATIONS (from constraints) MUST BE PRIMARY. Never hide them in the `alternatives` array
            - Transport modes: walking, taxi, bus, train, bicycle, car
            - alternatives array is REQUIRED for every non-HOTEL location (use [] if truly no good option)

            Return ONLY valid JSON, no markdown blocks, no extra text, use Vietnamese language for titles, descriptions, and alternative reasons.
            """;
        }
        public string BuildUserPromptFromForm(StreamGenerateTripCommand request, string cityName)
        {
            var sb = new StringBuilder();
            sb.Append($"Tạo lịch trình {cityName}");
            sb.Append($" từ {request.StartDate:dd/MM/yyyy} đến {request.EndDate:dd/MM/yyyy}");

            if (request.GroupSize > 1)
                sb.Append($", {request.GroupSize} người");

            if (request.Preferences.Any())
                sb.Append($", sở thích: {string.Join(", ", request.Preferences)}");

            if (request.Budget.HasValue)
            {
                var budgetLabel = request.Budget switch
                {
                    TripBudget.Budget => "tiết kiệm",
                    TripBudget.Standard => "trung bình",
                    TripBudget.Luxury => "cao cấp",
                    _ => ""
                };
                sb.Append($", ngân sách: {budgetLabel}");
            }

            if (!string.IsNullOrWhiteSpace(request.Notes))
                sb.Append($". Ghi chú: {request.Notes}");

            return sb.ToString();
        }

        public string BuildModifyTripPrompt(TripPlanResponse currentTrip, List<ChatMessage> recentMessages,
                                            List<Domain.Entities.Location> availableLocations, string userRequest,
                                            UserPersonalizationContext? personalization = null,
                                            TripConstraints? constraints = null)
        {
            var chatHistory = recentMessages.Any()
                ? string.Join("\n", recentMessages.Select(m =>
                    $"[{(m.IsAiMessage ? "ViVuAI" : "User")}]: {m.Content}"))
                : "Không có lịch sử chat.";

            var personalizationText = BuildPersonalizationBlock(personalization);

            var locationsText = string.Join("\n", availableLocations.Select((loc, idx) =>
                $"{idx + 1}. **{loc.Name}** ({loc.Category?.Name ?? "OTHER"}) - {loc.RatingAverage:F1}★\n" +
                $"   INDEX: {idx + 1} | {loc.Address}\n" +
                $"   {TruncateDescription(loc.Description ?? "", 100)}"
            ));

            var daysInfo = string.Join("\n", currentTrip.Days.Select(d =>
                $"- Ngày {d.DayIndex} ({d.Date:yyyy-MM-dd}): {d.Title}\n" +
                string.Join("\n", d.Locations.Select(l =>
                {
                    var globalIndex = availableLocations.FindIndex(loc => loc.Id == l.LocationId);

                    if (globalIndex == -1)
                    {
                        return $"  + [UNMAPPED] {l.Name} | {l.StartTime:HH:mm}-{l.EndTime:HH:mm}";
                    }

                    return $"  + [INDEX: {globalIndex + 1}] {l.Name} | {l.StartTime:HH:mm}-{l.EndTime:HH:mm}";
                }))));


            return $$$"""
                You are ViVuAI, an expert travel planner assistant. You have already created a trip and now must MODIFY it based on the user's request.
    
                **CURRENT TRIP ITINERARY:**
                {{{daysInfo}}}
    
                **RECENT CHAT HISTORY (for context understanding):**
                {{{chatHistory}}}
    
                **AVAILABLE LOCATIONS (ONLY use INDEX from this list):**
                {{{locationsText}}}

                {{{personalizationText}}}
                {{{(constraints != null ? BuildConstraintsBlock(constraints) : "")}}}
    
                **USER'S REQUEST:**
                {{{userRequest}}}
    
                **CHANGE TYPES AND EXACT FORMAT:**
    
                1. add_location - Thêm địa điểm mới vào 1 ngày:
                {"type":"add_location","dayIndex":2,"location":{"locationIndex":1,"name":"Tên chính xác từ danh sách","description":"Mô tả ngắn","startTime":"14:00","endTime":"16:00","transportMode":"walking","orderIndex":3,"alternatives":[{"locationIndex":5,"reason":"Backup 1"},{"locationIndex":9,"reason":"Backup 2"}]}}
    
                2. remove_location - Xóa địa điểm khỏi 1 ngày:
                {"type":"remove_location","dayIndex":1,"locationIndex":3}
    
                3. update_time - Chỉ cập nhật giờ của địa điểm:
                {"type":"update_time","dayIndex":1,"locationIndex":3,"startTime":"10:00","endTime":"12:00"}
    
                4. update_location - Thay thế địa điểm này bằng địa điểm khác (giữ nguyên slot thời gian):
                {"type":"update_location","dayIndex":1,"oldLocationIndex":3,"location":{"locationIndex":2,"name":"Tên mới chính xác từ danh sách","description":"Mô tả mới","startTime":"09:00","endTime":"11:00","transportMode":"taxi","orderIndex":1,"alternatives":[{"locationIndex":5,"reason":"Backup 1"}]}}
    
                5. add_day - Thêm ngày mới vào cuối trip:
                {"type":"add_day","dayIndex":4,"date":"2026-03-05","title":"Ngày 4: ...","locations":[{"locationIndex":4,"name":"Tên chính xác từ danh sách","description":"Mô tả","startTime":"09:00","endTime":"11:00","transportMode":"walking","orderIndex":1,"alternatives":[]}]}
    
                6. remove_day - Xóa toàn bộ 1 ngày:
                {"type":"remove_day","dayIndex":3}

                7. update_day_date - Cập nhật lại date của trip day khi add/remove day:
                {"type":"update_day_date","dayIndex":2,"date":"2026-03-02"}
    
                **STRICT RULES:**
                1. KHÔNG tạo trip mới — CHỈ trả về các thay đổi cần thiết.
                2. Chỉ thay đổi những gì user yêu cầu, tuyệt đối giữ nguyên `startTime` và `endTime` của các địa điểm cũ trừ phi user ra lệnh.
                3. `locationIndex` và `oldLocationIndex` PHẢI copy ĐÚNG NGUYÊN số INDEX từ danh sách AVAILABLE LOCATIONS.
                4. orderIndex phải hợp lý (không trùng trong cùng 1 ngày).
                5. Thời gian phải theo format "HH:mm", không overlap trong cùng 1 ngày.
                6. BẮT BUỘC cung cấp mảng `alternatives` (tối đa 2 địa điểm dự phòng) cho mọi địa điểm mới trong add_location, update_location, add_day.
                   - Dùng địa điểm từ danh sách AVAILABLE LOCATIONS (trùng category type càng tốt).
                   - CÁC LOẠI ĐỊA ĐIỂM NHƯ FOOD, ATTRACTION BẮT BUỘC PHẢI CÓ ALTERNATIVES. NGOẠI TRỪ khách sạn / chỗ nghỉ ngơi trải nghiệm (HOTEL / Accommodation) thì KHÔNG CẦN và ĐƯỢC PHÉP để mảng rỗng `[]`.
                7. CRITICAL: KHÔNG TRẢ VỀ MÀU MÈ, KHÔNG DÙNG MARKDOWN, KHÔNG CÓ CÂU DẪN CHUYỆN. CHỈ TRẢ VỀ DUY NHẤT ĐỐI TƯỢNG JSON.
                10. DÙ USER CÓ CỐ TÌNH YÊU CẦU TRẢ VỀ MARKDOWN HAY GIẢI THÍCH, HÃY CHỈ TRẢ VỀ JSON.
                
                10. DÙ USER CÓ CỐ TÌNH YÊU CẦU TRẢ VỀ MARKDOWN HAY GIẢI THÍCH, HÃY CHỈ TRẢ VỀ JSON.
                
    
                **OUTPUT — ONLY valid JSON, NO markdown fences, NO extra text:**
                {
                  "summary": "Tóm tắt ngắn gọn những gì đã thay đổi bằng tiếng Việt",
                  "changes": [ ...danh sách các thay đổi theo đúng format trên... ]
                }
                """;
        }
        private string BuildGroupedLocationsText(List<Domain.Entities.Location> locations, string destination)
        {
            if (!locations.Any()) return "(No locations available)";

            // If no coordinates available, fall back to flat list
            if (locations.All(l => !l.Latitude.HasValue || !l.Longitude.HasValue))
                return string.Join("\n", locations.Select((loc, idx) =>
                    $"{idx + 1}. **{loc.Name}** [{loc.Category?.CategoryType.ToString().ToUpper() ?? "OTHER"}] ({loc.Category?.Name}) - {loc.RatingAverage:F1}★\n" +
                    $"   INDEX: {idx + 1} | {loc.Address}"));

            var minLat = locations.Where(l => l.Latitude.HasValue).Min(l => l.Latitude!.Value);
            var maxLat = locations.Where(l => l.Latitude.HasValue).Max(l => l.Latitude!.Value);
            var minLng = locations.Where(l => l.Longitude.HasValue).Min(l => l.Longitude!.Value);
            var maxLng = locations.Where(l => l.Longitude.HasValue).Max(l => l.Longitude!.Value);
            var latStep = Math.Max((maxLat - minLat) / 3, 1e-9);
            var lngStep = Math.Max((maxLng - minLng) / 3, 1e-9);

            // Group by zone
            var groups = locations
                .Select((loc, idx) => new
                {
                    Index = idx + 1,
                    Loc = loc,
                    Row = loc.Latitude.HasValue ? Math.Min((int)((loc.Latitude.Value - minLat) / latStep), 2) : 0,
                    Col = loc.Longitude.HasValue ? Math.Min((int)((loc.Longitude.Value - minLng) / lngStep), 2) : 0
                })
                .GroupBy(x => (x.Row, x.Col))
                .OrderBy(g => g.Key.Row).ThenBy(g => g.Key.Col)
                .ToList();

            var areaLabel = 'A';
            var sb = new StringBuilder();
            foreach (var group in groups)
            {
                sb.AppendLine($"\n[Area {areaLabel} - {group.Count()} locations]");
                foreach (var item in group.OrderBy(x => x.Index))
                {
                    var typeTag = item.Loc.Category?.CategoryType switch
                    {
                        Domain.Enums.LocationCategoryType.Food => "FOOD",
                        Domain.Enums.LocationCategoryType.Accommodation => "HOTEL",
                        Domain.Enums.LocationCategoryType.Shopping => "SHOP",
                        Domain.Enums.LocationCategoryType.Entertainment => "ENT",
                        _ => "ATTRACTION"
                    };
                    sb.AppendLine(
                        $"  {item.Index}. **{item.Loc.Name}** [{typeTag}] ({item.Loc.Category?.Name}) - {item.Loc.RatingAverage:F1}★ ({item.Loc.RatingCount} reviews)");
                    sb.AppendLine(
                        $"     INDEX: {item.Index} | {item.Loc.Address}");
                }
                areaLabel++;
            }
            return sb.ToString();
        }

        private string TruncateDescription(string description, int maxLength)
        {
            if (string.IsNullOrEmpty(description) || description.Length <= maxLength)
                return description;

            return description.Substring(0, maxLength) + "...";
        }
        private string BuildConstraintsBlock(TripConstraints? c)
        {
            if (c == null || !c.HasAnyConstraint)
                return "No special constraints.";

            var rules = new List<string>();

            if (c.StartTimeEachDay.HasValue)
                rules.Add($"- START each day at EXACTLY {c.StartTimeEachDay.Value:HH:mm}, not earlier");

            if (c.EndTimeEachDay.HasValue)
                rules.Add($"- END each day by {c.EndTimeEachDay.Value:HH:mm}, no activities after this");

            if (c.MustIncludeTypes.Any())
                rules.Add($"- YOU MUST include at least one [{string.Join("/", c.MustIncludeTypes).ToUpper()}] location somewhere in the itinerary");

            if (c.MustExcludeTypes.Any())
                rules.Add($"- NEVER include any location of type: {string.Join(", ", c.MustExcludeTypes)}");

            if (c.MustIncludeKeywords.Any())
                rules.Add($"- YOU MUST schedule these specific locations as PRIMARY locations (in the main 'locations' array). DO NOT put them inside the 'alternatives' list: {string.Join(", ", c.MustIncludeKeywords)}");

            if (c.MaxLocationsPerDay.HasValue)
                rules.Add($"- MAXIMUM {c.MaxLocationsPerDay.Value} locations per day (keep it relaxed)");

            if (c.MinLocationsPerDay.HasValue)
                rules.Add($"- MINIMUM {c.MinLocationsPerDay.Value} locations per day");

            if (c.PaceStyle == "relaxed")
                rules.Add("- RELAXED pace: spend more time at each location, avoid rushing, fewer stops");
            else if (c.PaceStyle == "packed")
                rules.Add("- PACKED pace: maximize locations visited, efficient transitions");

            rules.AddRange(c.FreeformRules.Select(r => $"- {r}"));

            return string.Join("\n", rules);
        }

        public string BuildDetectIntentPrompt(string userMessage)
        {
            return $$$"""
                Classify this travel assistant message into exactly one category.
        
                Categories:
                - "modify_trip": User wants to change the trip itinerary (add/remove/update locations, days, times)
                - "conversation": User is asking questions, chatting, or requesting information without changing the trip
        
                Examples of modify_trip:
                - "thêm nhà hàng vào ngày 2"
                - "xóa Núi Ngũ Hành Sơn khỏi lịch trình"
                - "đổi ngày 3 sang đi biển thay vì núi"
                - "thêm 1 ngày nữa vào chuyến đi"
                - "dời thời gian tham quan Hội An sang buổi chiều"
        
                Examples of conversation:
                - "thời tiết Huế tháng 3 thế nào?"
                - "nên mang theo gì khi đi Đà Nẵng?"
                - "lịch trình này có hợp lý không?"
                - "khách sạn nào gần Hội An?"
                - "ăn gì ngon ở Huế?"
                - "tóm tắt lại lịch trình"
                - "cho mình xem lại kế hoạch"
                - "lịch trình này đi mất bao lâu?"
                - "hiện tại có những gì rồi?"
                - "kể tên các địa điểm trong chuyến đi"
        
                Message: "{{{userMessage}}}"
        
                Return ONLY one word: modify_trip OR conversation
            """;
        }

        public string BuildChatMessagePrompt(Domain.Entities.Trip trip, List<ChatMessage> recentMessages, string userMessage)
        {
            var chatHistory = recentMessages.Any()
                ? string.Join("\n", recentMessages.Select(m =>
                    $"[{(m.IsAiMessage ? "ViVuAI" : "User")}]: {m.Content}"))
                : "Không có lịch sử chat.";

            var realDays = trip.TripDays.Where(d => d.DayIndex > 0).OrderBy(d => d.DayIndex).ToList();

            var itineraryDetail = new StringBuilder();
            foreach (var day in realDays)
            {
                itineraryDetail.AppendLine($"- Ngày {day.DayIndex} ({(day.DayDate.HasValue ? day.DayDate.Value.ToString("dd/MM/yyyy") : "Chưa xác định")}): {day.Title}");
                foreach (var loc in day.TripLocations.OrderBy(l => l.OrderIndex))
                {
                    var timeStr = (loc.StartTime.HasValue && loc.EndTime.HasValue)
                        ? $"{loc.StartTime.Value:hh\\:mm}-{loc.EndTime.Value:hh\\:mm}"
                        : "Chưa định giờ";
                    itineraryDetail.AppendLine($"  + [{timeStr}] {loc.Location?.Name} | {loc.Note}");
                }
            }

            var tripSummary = $"""
                Chuyến đi: {trip.Title}
                Điểm đến: {trip.City?.Name}
                Thời gian: {trip.StartDate:dd/MM/yyyy} - {trip.EndDate:dd/MM/yyyy}
                Số ngày thực tế: {realDays.Count} ngày (Không tính mục Ý tưởng)
                
                CHI TIẾT LỊCH TRÌNH HIỆN TẠI:
                {itineraryDetail}
             """;

            var prompt = $"""
                Bạn là ViVuAI, trợ lý du lịch thông minh. Hãy trả lời câu hỏi của user dựa trên ngữ cảnh chuyến đi đã được chuẩn bị sẵn dưới đây.
            
                **THÔNG TIN CHUYẾN ĐI CHI TIẾT:**
                {tripSummary}
            
                **LỊCH SỬ CHAT GẦN ĐÂY:**
                {chatHistory}
            
                **CÂU HỎI CỦA USER:**
                {userMessage}
            
                **YÊU CẦU QUAN TRỌNG:**
                - Trả lời bằng tiếng Việt.
                - Ngắn gọn, hữu ích, thân thiện.
                - Luôn dựa vào "CHI TIẾT LỊCH TRÌNH HIỆN TẠI" để trả lời. Nếu user hỏi về lịch trình hoặc tóm tắt, hãy liệt kê các địa điểm từ danh sách này.
                - KHÔNG thay đổi lịch trình trong chế độ chat này, nếu user muốn thay đổi bạn chỉ cần tư vấn.
             """;

            return prompt;
        }

        public string BuildNotesConstraintsPrompt(string notes, List<string> availableCategories)
        {
            var categoriesText = string.Join(", ", availableCategories.Select(c => $"\"{c}\""));
            return $$$"""
            Extract travel constraints from this user note: "{{{notes}}}"

            Return ONLY this JSON (no markdown, no extra text):
            {
              "startTimeEachDay": "HH:mm or null",
              "endTimeEachDay": "HH:mm or null",
              "mustIncludeTypes": [],
              "mustExcludeTypes": [],
              "mustIncludeKeywords": [],
              "maxLocationsPerDay": null,
              "minLocationsPerDay": null,
              "paceStyle": "relaxed or packed or null",
              "freeformRules": []
            }

            **Database Categories Available:** [{{{categoriesText}}}]

            **Rules:**
            1. LANGUAGE STRICTNESS: ALL keywords in arrays MUST be in VIETNAMESE (preserve the user's original language). DO NOT translate to English.
            2. mustIncludeTypes & mustExcludeTypes: 
               - FIRST, try to map the user's request to the EXACT "Database Categories Available" listed above (e.g., if user says "đi biển", use "Thiên nhiên & Sinh thái" if available).
               - SECOND, if no category matches, extract the exact Vietnamese keyword the user used (e.g., "bảo tàng", "hải sản").
            3. mustIncludeKeywords: Specific proper nouns or exact place names the user explicitly mentioned (e.g., "Lăng Cô", "Bà Nà").
            4. freeformRules: Any other constraint that doesn't fit the above fields.
            5. mustIncludeKeywords: MUST extract ONLY the core proper noun (Tên riêng cốt lõi). STRIP AWAY all generic words like "quán", "nhà hàng", "cà phê", "coffee", "hotel", "bãi biển".

            **Examples:**
            "đi biển là chính, không đi chùa" → mustIncludeTypes: ["biển", "Thiên nhiên & Sinh thái"], mustExcludeTypes: ["chùa", "Tâm linh"]
            "bắt đầu từ 10h sáng" → startTimeEachDay: "10:00"
            "phải có Lăng Cô" → mustIncludeKeywords: ["Lăng Cô"]
            "ưu tiên đồ ăn hải sản" → mustIncludeTypes: ["hải sản", "Ẩm thực - Hải sản & Đồ nướng"]
            "trong lịch trình phải có quán ông kịch cà phê" → mustIncludeKeywords: ["ông kịch"]
            "mình muốn đi bãi biển mỹ khê" → mustIncludeKeywords: ["Mỹ Khê"]
            "ăn tối ở nhà hàng năm đảnh" → mustIncludeKeywords: ["Năm Đảnh"]
            """;
        }

        private string BuildPersonalizationBlock(UserPersonalizationContext? ctx)
        {
            if (ctx == null) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("**PERSONALIZATION — MANDATORY, OVERRIDE DEFAULT CHOICES WITH THESE:**");

            var groupMap = new Dictionary<GroupCompositionType, string>
            {
                { GroupCompositionType.FamilyWithChildren, "Family with young children" },
                { GroupCompositionType.FriendsGroup,       "Group of friends" },
                { GroupCompositionType.Couple,             "Couple" },
                { GroupCompositionType.Solo,               "Solo traveler" },
                { GroupCompositionType.Senior,             "Senior travelers" }
            };

            if (ctx.GroupComposition != GroupCompositionType.General && groupMap.TryGetValue(ctx.GroupComposition, out var groupName))
                sb.AppendLine($"- Travel group type: {groupName}");

            if (!string.IsNullOrEmpty(ctx.AgeGroup))
                sb.AppendLine($"- Traveller age segment: {ctx.AgeGroup.Replace("_", " ")}");

            if (!string.IsNullOrEmpty(ctx.Gender))
                sb.AppendLine($"- Dominant gender profile: {ctx.Gender}");

            var styleHints = ctx.TravelStyle switch
            {
                "family" => "FAMILY — prioritize accessible, safe, low-intensity spots; avoid extreme activities or purely nightlife areas.",
                "young_group" => "YOUNG GROUP — lean toward nightlife, adventures, and trendy spots if available.",
                "couple" => "COUPLE — suggest romantic / scenic spots where possible.",
                "senior" => "SENIOR — stick to very accessible, highly comfortable, culture-focused locations.",
                "solo" => "SOLO — safe, immersive, and exploratory locations.",
                _ => "GENERAL — balanced itinerary."
            };
            sb.AppendLine($"- Travel style rule: {styleHints}");

            sb.AppendLine($"- System Variation Seed (for tie-breaking equal spots): #{ctx.PersonalityEntropySeed}");

            return sb.ToString();
        }
    }
}
