import { describe, expect, it } from "vitest";
import IncrementalTripFormatter from "../IncrementalTripFormatter";

describe("IncrementalTripFormatter", () => {
  it("returns full accumulated text instead of delta", () => {
    const formatter = new IncrementalTripFormatter();

    const firstOutput = formatter.addChunk('{"title":"Trip to Hanoi"');
    const secondOutput = formatter.addChunk(',"description":"Explore food"');

    expect(firstOutput).toContain("Trip to Hanoi");
    expect(secondOutput).toContain("Trip to Hanoi");
    expect(secondOutput).toContain("Explore food");
    expect(secondOutput.length).toBeGreaterThan(firstOutput.length);
  });

  it("finalize appends success message onto accumulated content", () => {
    const formatter = new IncrementalTripFormatter();

    formatter.addChunk(
      '{"title":"Trip to Hanoi","description":"Explore food"}',
    );
    const finalOutput = formatter.finalize();

    expect(finalOutput).toContain("Trip to Hanoi");
    expect(finalOutput).toContain("✅ Lịch trình đã sẵn sàng!");
  });

  it("never shrinks once a richer formatted output has been produced", () => {
    const formatter = new IncrementalTripFormatter();

    const firstOutput = formatter.addChunk(
      '{"title":"Trip to Hanoi","description":"Explore food","days":[{"dayIndex":1,"title":"Ngày 1"',
    );
    const secondOutput = formatter.addChunk("}");

    expect(secondOutput.length).toBeGreaterThanOrEqual(firstOutput.length);
  });

  it("extracts locations when LocationId is the first property (backend format)", () => {
    const formatter = new IncrementalTripFormatter();

    const json =
      '{"Title":"Hành trình Hà Nội","Description":"Khám phá thủ đô","Start":"2026-03-13","End":"2026-03-15",' +
      '"Days":[{"DayIndex":1,"Title":"Ngày 1: Lịch sử","Locations":[' +
      '{"LocationId":"aaa-111","Name":"Lăng Chủ tịch","Description":"Viếng lăng Bác","StartTime":"08:00:00"},' +
      '{"LocationId":"bbb-222","Name":"Văn Miếu","Description":"Trường đại học đầu tiên","StartTime":"10:00:00"}' +
      "]}]}";

    const output = formatter.addChunk(json);

    // Verify locations are extracted
    expect(output).toContain("Lăng Chủ tịch");
    expect(output).toContain("Viếng lăng Bác");
    expect(output).toContain("08:00");
    expect(output).toContain("Văn Miếu");
    expect(output).toContain("Trường đại học đầu tiên");
    expect(output).toContain("10:00");
    // Verify day header
    expect(output).toContain("Ngày 1: Lịch sử");
  });

  it("returns currentOutput for falsy chunk instead of empty string", () => {
    const formatter = new IncrementalTripFormatter();

    formatter.addChunk('{"title":"My Trip"}');
    const afterNull = formatter.addChunk(null);
    const afterEmpty = formatter.addChunk("");

    expect(afterNull).toContain("My Trip");
    expect(afterEmpty).toContain("My Trip");
  });
});
