using System;
using System.Collections.Generic;
using System.Text;
using BareORM.Migrations.Abstractions;
using BareORM.Migrations.DbAssets;
using BareORM.Migrations.Snapshot;

namespace BareORM.Migrations.Diff
{
    public sealed class ProgrammablesDiffer
    {
        // MVP seguro:
        // - solo crea/actualiza lo que declarás en assets
        // - NO borra nada automáticamente
        public IReadOnlyList<MigrationOperation> Diff(
            ProgrammablesSnapshot oldSnap,
            IEnumerable<DbAsset> currentAssets)
        {
            var ops = new List<MigrationOperation>();

            var old = oldSnap.Items.ToDictionary(
                x => Key(x.Schema, x.Name, x.Kind),
                x => x,
                StringComparer.OrdinalIgnoreCase);

            foreach (var a in currentAssets)
            {
                var hash = AssetHasher.Hash(a.Sql);
                var k = Key(a.Schema, a.Name, (int)a.Kind);

                if (!old.TryGetValue(k, out var prev) || !string.Equals(prev.Hash, hash, StringComparison.OrdinalIgnoreCase))
                {
                    ops.Add(ToCreateOrAlterOp(a));
                }
            }

            return ops;
        }

        private static string Key(string schema, string name, int kind) => $"{schema}.{name}::{kind}";

        private static MigrationOperation ToCreateOrAlterOp(DbAsset a)
        {
            return a.Kind switch
            {
                DbAssetKind.Trigger => new CreateOrAlterTriggerOp(a.Schema, a.Name, a.Sql),
                DbAssetKind.View => new CreateOrAlterViewOp(a.Schema, a.Name, a.Sql),
                DbAssetKind.Procedure => new CreateOrAlterRoutineOp(a.Schema, a.Name, RoutineKind.Procedure, a.Sql),
                DbAssetKind.ScalarFunction => new CreateOrAlterRoutineOp(a.Schema, a.Name, RoutineKind.ScalarFunction, a.Sql),
                DbAssetKind.TableFunction => new CreateOrAlterRoutineOp(a.Schema, a.Name, RoutineKind.TableFunction, a.Sql),
                _ => new SqlOp(a.Sql)
            };
        }
    }
}
