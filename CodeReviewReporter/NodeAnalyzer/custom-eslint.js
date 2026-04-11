const { ESLint } = require("eslint");
const noConsoleLogRule = require("./rules/no-console-log");

(async function main() {
    const eslint = new ESLint({
        useEslintrc: true,
        overrideConfig: {
            rules: {
                "no-console-log": "warn"
            }
        },
        plugins: {
            custom: {
                rules: {
                    "no-console-log": noConsoleLogRule
                }
            }
        }
    });

    const results = await eslint.lintFiles(process.argv.slice(2));
    console.log(JSON.stringify(results));
})();