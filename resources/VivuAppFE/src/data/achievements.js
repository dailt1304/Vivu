export const ACHIEVEMENTS = [
  {
    id: "first_trip",
    icon: "🎒",
    title: "Bắt đầu hành trình",
    description: "Tạo chuyến đi đầu tiên",
    condition: (stats) => stats.trips >= 1,
    target: 1,
    category: "trips",
  },
  {
    id: "explorer_5",
    icon: "🧭",
    title: "Nhà thám hiểm",
    description: "Hoàn thành 5 chuyến đi",
    condition: (stats) => stats.trips >= 5,
    target: 5,
    category: "trips",
  },
  {
    id: "adventurer",
    icon: "🗺️",
    title: "Phượt thủ",
    description: "Hoàn thành 10 chuyến đi",
    condition: (stats) => stats.trips >= 10,
    target: 10,
    category: "trips",
  },
  {
    id: "beach_lover",
    icon: "🌊",
    title: "Người yêu biển",
    description: "Ghé thăm 5 bãi biển",
    condition: (stats) => stats.beachVisits >= 5,
    target: 5,
    category: "places",
  },
  {
    id: "mountain_explorer",
    icon: "🏔️",
    title: "Chinh phục đỉnh cao",
    description: "Đến 3 điểm núi",
    condition: (stats) => stats.mountainVisits >= 3,
    target: 3,
    category: "places",
  },
  {
    id: "city_hopper",
    icon: "🏙️",
    title: "Khám phá đô thị",
    description: "Ghé thăm 10 thành phố",
    condition: (stats) => stats.cityVisits >= 10,
    target: 10,
    category: "places",
  },
  {
    id: "first_blog",
    icon: "✍️",
    title: "Người kể chuyện",
    description: "Viết bài blog đầu tiên",
    condition: (stats) => stats.blogs >= 1,
    target: 1,
    category: "content",
  },
  {
    id: "blogger_10",
    icon: "📝",
    title: "Blogger chuyên nghiệp",
    description: "Viết 10 bài blog",
    condition: (stats) => stats.blogs >= 10,
    target: 10,
    category: "content",
  },
  {
    id: "first_review",
    icon: "⭐",
    title: "Nhà phê bình",
    description: "Viết đánh giá đầu tiên",
    condition: (stats) => stats.reviews >= 1,
    target: 1,
    category: "content",
  },
  {
    id: "top_reviewer",
    icon: "🏆",
    title: "Reviewer chuyên nghiệp",
    description: "Đánh giá 20 địa điểm",
    condition: (stats) => stats.reviews >= 20,
    target: 20,
    category: "content",
  },
  {
    id: "photographer",
    icon: "📸",
    title: "Nhiếp ảnh gia",
    description: "Chia sẻ 50 ảnh",
    condition: (stats) => stats.photos >= 50,
    target: 50,
    category: "content",
  },
  {
    id: "local_expert",
    icon: "🎯",
    title: "Chuyên gia địa phương",
    description: "Khám phá 20 tỉnh thành",
    condition: (stats) => stats.visitedProvinces >= 20,
    target: 20,
    category: "exploration",
  },
  {
    id: "vietnam_master",
    icon: "🇻🇳",
    title: "Bậc thầy Việt Nam",
    description: "Khám phá 50 tỉnh thành",
    condition: (stats) => stats.visitedProvinces >= 50,
    target: 50,
    category: "exploration",
  },
  {
    id: "social_butterfly",
    icon: "🦋",
    title: "Người nổi tiếng",
    description: "Có 100 người theo dõi",
    condition: (stats) => stats.followers >= 100,
    target: 100,
    category: "social",
  },
  {
    id: "influencer",
    icon: "👑",
    title: "Influencer",
    description: "Có 1000 người theo dõi",
    condition: (stats) => stats.followers >= 1000,
    target: 1000,
    category: "social",
  },
];

// Helper to check unlocked achievements
export const getUnlockedAchievements = (stats) => {
  return ACHIEVEMENTS.filter((achievement) => achievement.condition(stats));
};

// Helper to get achievement progress
export const getAchievementProgress = (achievement, stats) => {
  const categoryMappings = {
    trips: stats.trips,
    beachVisits: stats.beachVisits,
    mountainVisits: stats.mountainVisits,
    cityVisits: stats.cityVisits,
    blogs: stats.blogs,
    reviews: stats.reviews,
    photos: stats.photos,
    visitedProvinces: stats.visitedProvinces,
    followers: stats.followers,
  };

  // Find the relevant stat based on condition
  let current = 0;
  if (achievement.id.includes("trip")) current = stats.trips || 0;
  else if (achievement.id.includes("beach")) current = stats.beachVisits || 0;
  else if (achievement.id.includes("mountain"))
    current = stats.mountainVisits || 0;
  else if (achievement.id.includes("city")) current = stats.cityVisits || 0;
  else if (achievement.id.includes("blog")) current = stats.blogs || 0;
  else if (achievement.id.includes("review")) current = stats.reviews || 0;
  else if (achievement.id.includes("photo")) current = stats.photos || 0;
  else if (
    achievement.id.includes("vietnam") ||
    achievement.id.includes("local")
  )
    current = stats.visitedProvinces || 0;
  else if (
    achievement.id.includes("social") ||
    achievement.id.includes("influencer")
  )
    current = stats.followers || 0;

  return {
    current,
    target: achievement.target,
    percentage: Math.min(100, (current / achievement.target) * 100),
  };
};
