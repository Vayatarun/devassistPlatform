module.exports = {
    meta: {
        type: "problem",
        docs: {
            description: "Disallow console.log in production"
        },
        messages: {
            noConsole: "Avoid using console.log in production code."
        }
    },
    create(context) {
        return {
            CallExpression(node) {
                if (
                    node.callee.object &&
                    node.callee.object.name === "console" &&
                    node.callee.property &&
                    node.callee.property.name === "log"
                ) {
                    context.report({
                        node,
                        messageId: "noConsole"
                    });
                }
            }
        };
    }
};