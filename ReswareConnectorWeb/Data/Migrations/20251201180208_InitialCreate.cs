using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReswareConnectorWeb.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    transaction_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    transaction_type_id = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    file_number = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    data_path = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    received_time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    processed = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.transaction_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TransactionItems",
                columns: table => new
                {
                    transaction_item_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    transaction_type_id = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    processed = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    retry_count = table.Column<byte>(type: "TINYINT UNSIGNED", nullable: true),
                    response_sent = table.Column<ulong>(type: "BIT(1)", nullable: true),
                    last_updated_time = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionItems", x => x.transaction_item_id);
                    table.ForeignKey(
                        name: "FK_TransactionItems_Transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "Transactions",
                        principalColumn: "transaction_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TransactionResponses",
                columns: table => new
                {
                    transaction_response_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    transaction_item_id = table.Column<long>(type: "bigint", nullable: false),
                    received_time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    response_code = table.Column<int>(type: "int", nullable: false),
                    response_message = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionResponses", x => x.transaction_response_id);
                    table.ForeignKey(
                        name: "FK_TransactionResponses_TransactionItems_transaction_item_id",
                        column: x => x.transaction_item_id,
                        principalTable: "TransactionItems",
                        principalColumn: "transaction_item_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_transaction_items_processed",
                table: "TransactionItems",
                column: "processed");

            migrationBuilder.CreateIndex(
                name: "idx_transaction_items_transaction_id",
                table: "TransactionItems",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "idx_transaction_items_transaction_processed",
                table: "TransactionItems",
                columns: new[] { "transaction_id", "processed" });

            migrationBuilder.CreateIndex(
                name: "idx_transaction_items_type_id",
                table: "TransactionItems",
                column: "transaction_type_id");

            migrationBuilder.CreateIndex(
                name: "idx_transaction_responses_code",
                table: "TransactionResponses",
                column: "response_code");

            migrationBuilder.CreateIndex(
                name: "idx_transaction_responses_item_id",
                table: "TransactionResponses",
                column: "transaction_item_id");

            migrationBuilder.CreateIndex(
                name: "idx_transaction_responses_received_time",
                table: "TransactionResponses",
                column: "received_time");

            migrationBuilder.CreateIndex(
                name: "idx_transactions_file_number",
                table: "Transactions",
                column: "file_number");

            migrationBuilder.CreateIndex(
                name: "idx_transactions_processed",
                table: "Transactions",
                column: "processed");

            migrationBuilder.CreateIndex(
                name: "idx_transactions_received_time",
                table: "Transactions",
                column: "received_time");

            migrationBuilder.CreateIndex(
                name: "idx_transactions_type_id",
                table: "Transactions",
                column: "transaction_type_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransactionResponses");

            migrationBuilder.DropTable(
                name: "TransactionItems");

            migrationBuilder.DropTable(
                name: "Transactions");
        }
    }
}
