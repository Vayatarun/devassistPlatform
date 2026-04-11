module.exports = [
  {
    files: ["**/*.js"],
    languageOptions: {
      ecmaVersion: "latest"
    },
    rules: {
      "semi": ["error", "always"],
      "no-var": "error",
      "no-unused-vars": "warn"
    }
  }
];