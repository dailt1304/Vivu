/**
 * IncrementalTripFormatter
 *
 * This utility class incrementally parses a streamed JSON string containing
 * trip plan data and formats it into markdown on the fly.
 * It keeps track of characters that have already been formatted and returned
 * so that subsequent calls only return the "new" formatted text.
 *
 * Also provides a static `formatFromParsedData(tripPlan)` method as a robust
 * fallback — it generates the same markdown from a properly JSON-parsed object
 * instead of regex-based extraction (guaranteed correct when stream is complete).
 */
class IncrementalTripFormatter {
  constructor() {
    this.buffer = "";
    this.isFinished = false;
    this.currentOutput = "";
  }

  /**
   * Add a chunk of raw JSON text and get back the FULL accumulated markdown.
   * @param {string} chunk
   * @returns {string} The current accumulated markdown text
   */
  addChunk(chunk) {
    if (!chunk) return this.currentOutput;
    this.buffer += chunk;

    const fullFormatted = this._formatFromBuffer(this.buffer);
    if (fullFormatted.length >= this.currentOutput.length) {
      this.currentOutput = fullFormatted;
    }

    return this.currentOutput;
  }

  /**
   * Called when the stream is completely done.
   * Optionally accepts a parsed tripPlan object to regenerate markdown if
   * the regex-based output is incomplete (e.g. missing locations).
   * @param {Object} [tripPlan] - Parsed trip plan from `complete` event
   * @returns {string} The final accumulated markdown text
   */
  finalize(tripPlan) {
    if (this.isFinished) {
      return this.currentOutput;
    }

    // If a parsed tripPlan is provided, check whether the regex-based output
    // is incomplete (has days with locations but output is missing them)
    if (tripPlan) {
      const totalExpectedLocations = (tripPlan.Days || []).reduce(
        (sum, d) => sum + (d.Locations?.length || 0),
        0,
      );
      // Count rendered locations by the numbered bold pattern **N. Name**
      const renderedLocations = (
        this.currentOutput.match(/\*\*\d+\.\s/g) || []
      ).length;

      if (totalExpectedLocations > 0 && renderedLocations < totalExpectedLocations) {
        // Regex failed to parse locations — regenerate from parsed data
        this.currentOutput =
          IncrementalTripFormatter.formatFromParsedData(tripPlan);
      }
    }

    const finalMsg =
      "\n\n✅ Lịch trình đã sẵn sàng! Bạn có thể chỉnh sửa trong phần Chi tiết.";
    this.isFinished = true;
    this.currentOutput = `${this.currentOutput}${finalMsg}`;
    return this.currentOutput;
  }

  /**
   * Generate markdown from a fully-parsed tripPlan object.
   * This is a reliable fallback that doesn't depend on regex extraction.
   *
   * @param {Object} tripPlan - Parsed trip plan (PascalCase keys from backend)
   * @returns {string} Formatted markdown
   */
  static formatFromParsedData(tripPlan) {
    if (!tripPlan) return "";
    let result = "";

    if (tripPlan.Title) {
      result += `✨ **${tripPlan.Title}**\n\n`;
    }
    if (tripPlan.Description) {
      result += `${tripPlan.Description}\n\n`;
    }
    if (tripPlan.Start && tripPlan.End) {
      result += `📅 ${tripPlan.Start} - ${tripPlan.End}\n\n`;
    } else if (tripPlan.Start) {
      result += `📅 Từ ${tripPlan.Start}\n\n`;
    }

    if (tripPlan.Days?.length) {
      tripPlan.Days.forEach((day, dayIdx) => {
        const dayTitle = day.Title || `Ngày ${dayIdx + 1}`;
        result += `---\n### ${dayTitle}\n\n`;

        if (day.Locations?.length) {
          day.Locations.forEach((loc, locIdx) => {
            const name = loc.Name || "Địa điểm";
            const time = loc.StartTime
              ? `(${String(loc.StartTime).slice(0, 5)})`
              : "";
            result += `**${locIdx + 1}. ${name}** ${time}\n`;
            if (loc.Description) {
              result += `   ${loc.Description}\n`;
            }
            result += "\n";
          });
        }
      });
    }

    return result;
  }

  /**
   * Internal logic to extract strings from partial JSON and format them.
   * We use Regex to robustly find fully-closed string values.
   */
  _formatFromBuffer(str) {
    let result = "";

    // 1. Extract Title
    // Regex matches "Title": "something" or "title":"something"
    const titleMatch = str.match(/"[Tt]itle"\s*:\s*"((?:[^"\\]|\\.)*)"/);
    if (titleMatch) {
      const title = this._unescape(titleMatch[1]);
      result += `✨ **${title}**\n\n`;
    }

    // 2. Extract Description
    const descMatch = str.match(/"[Dd]escription"\s*:\s*"((?:[^"\\]|\\.)*)"/);
    if (descMatch) {
      const desc = this._unescape(descMatch[1]);
      result += `${desc}\n\n`;
    }

    // 3. Extract Start and End dates
    const startMatch = str.match(/"[Ss]tart"\s*:\s*"((?:[^"\\]|\\.)*)"/);
    const endMatch = str.match(/"[Ee]nd"\s*:\s*"((?:[^"\\]|\\.)*)"/);
    if (startMatch && endMatch) {
      result += `📅 ${this._unescape(startMatch[1])} - ${this._unescape(endMatch[1])}\n\n`;
    } else if (startMatch) {
      result += `📅 Từ ${this._unescape(startMatch[1])}\n\n`;
    }

    // 4. Extract Days Array
    // Find the content inside "Days": [...]
    const daysArrMatch = str.match(/"[Dd]ays"\s*:\s*\[([\s\S]*)/);
    if (daysArrMatch) {
      const daysStr = daysArrMatch[1];

      // Split into individual day blocks. We do this by looking for '{"DayIndex"' or '{"dayIndex"'
      const dayBlocks = daysStr.split(/\{\s*"[Dd]ayIndex"/).slice(1);

      dayBlocks.forEach((dayBlock, dayIdx) => {
        // Find day title
        const dayTitleMatch = dayBlock.match(
          /"[Tt]itle"\s*:\s*"((?:[^"\\]|\\.)*)"/,
        );
        const dayTitle = dayTitleMatch
          ? this._unescape(dayTitleMatch[1])
          : `Ngày ${dayIdx + 1}`;

        result += `---\n### ${dayTitle}\n\n`;

        // Find Locations array within this day block.
        // Use greedy match to grab everything after "Locations": [ until
        // end of the day block. Lazy *? was stopping at ] inside nested
        // "alternatives": [] — capturing only the first location.
        const locsArrMatch = dayBlock.match(
          /"[Ll]ocations"\s*:\s*\[([\s\S]*)/,
        );
        if (locsArrMatch) {
          const locsStr = locsArrMatch[1];
          // Split into location blocks by LocationId or LocationIndex
          const locBlocks = locsStr.split(/\{\s*"[Ll]ocation(?:[Ii]d|[Ii]ndex)"/).slice(1);

          locBlocks.forEach((locBlock, locIdx) => {
            // Reattach a generic key so inner regex can still find Name, StartTime, etc.
            const blockContent = '"_loc"' + locBlock;

            // Extract location details
            const nameMatch = blockContent.match(
              /"[Nn]ame"\s*:\s*"((?:[^"\\]|\\.)*)"/,
            );
            const timeMatch = blockContent.match(
              /"[Ss]tartTime"\s*:\s*"((?:[^"\\]|\\.)*)"/,
            );
            const lDescMatch = blockContent.match(
              /"[Dd]escription"\s*:\s*"((?:[^"\\]|\\.)*)"/,
            );

            if (nameMatch) {
              const name = this._unescape(nameMatch[1]);
              const time = timeMatch
                ? this._unescape(timeMatch[1]).slice(0, 5)
                : "";
              const lDesc = lDescMatch ? this._unescape(lDescMatch[1]) : "";

              result += `**${locIdx + 1}. ${name}** ${time ? `(${time})` : ""}\n`;
              if (lDesc) {
                result += `   ${lDesc}\n`;
              }
              result += "\n";
            }
          });
        }
      });
    }

    return result;
  }

  _unescape(str) {
    if (!str) return "";
    return str.replace(/\\n/g, "\n").replace(/\\"/g, '"');
  }
}

export default IncrementalTripFormatter;
