export const INSPIRATIONAL_QUOTES = [
  {
    text: "Đích đến không phải là một vùng đất, mà là một cách nhìn mới.",
    author: "Henry Miller",
  },
  {
    text: "Mỗi cuộc hành trình hàng dặm đều bắt đầu bằng một bước chân nhỏ bé.",
    author: "Lão Tử",
  },
  {
    text: "Thế giới là một cuốn sách, và ai không đi thì chỉ đọc được một trang.",
    author: "St. Augustine",
  },
  {
    text: "20 năm nữa bạn sẽ thất vọng vì những thứ bạn chưa làm hơn là những thứ bạn đã làm. Vì vậy hãy tung dây ra, giương buồm ra khỏi bến cảng an toàn. Đón gió vào cánh buồm của bạn. Khám phá. Ước mơ. Khám phá.",
    author: "Mark Twain",
  },
  {
    text: "Chỉ khi nào đi một mình trong im lặng, không hành lý, ta mới có thể đi vào trái tim của sự hoang dã thực sự.",
    author: "John Muir",
  },
  {
    text: "Chúng ta đi lang thang để tìm kiếm sự phân tâm, nhưng chúng ta đi du lịch để tìm lại chính mình.",
    author: "Hilaire Belloc",
  },
  {
    text: "Không phải tất cả những ai lang thang đều lạc lối.",
    author: "J.R.R. Tolkien",
  },
  {
    text: "Cuộc hành trình không phải là những dặm đường mà là những người bạn.",
    author: "Tim Cahill",
  },
  {
    text: "Đừng nói với tôi bạn học được bao nhiêu. Hãy kể cho tôi nghe bạn đã đi được bao xa.",
    author: "Mohammed",
  },
  {
    text: "Đầu tư vào du lịch là khoản đầu tư cho bản thân bạn.",
    author: "Matthew Karsten",
  },
  {
    text: "Đôi khi bạn chỉ cần để gió thổi bay mái tóc, và trái tim thổi bay những muộn phiền.",
    author: "Khuyết danh",
  },
  {
    text: "Tự do thực sự là khi không còn gì trói buộc đôi chân ở một chốn quen.",
    author: "Khuyết danh",
  },
  {
    text: "Góp nhặt kỷ niệm, xin đừng mang theo đồ đạc.",
    author: "Khuyết danh",
  },
  {
    text: "Những gì ta trải qua trên mỗi cung đường chính là những bài học không trường lớp nào dạy được.",
    author: "Khuyết danh",
  },
  {
    text: "Đi đâu không quan trọng, quan trọng là đi cùng ai.",
    author: "Khuyết danh",
  },
];

export const getRandomQuote = () => {
  return INSPIRATIONAL_QUOTES[
    Math.floor(Math.random() * INSPIRATIONAL_QUOTES.length)
  ];
};
