-- Run in the Supabase SQL editor (or via migration) after AuthUserId exists on Users.
-- Policies use auth.uid() = Users.AuthUserId.

-- Enable RLS on all app tables (safe to re-run)
ALTER TABLE "Users" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "SavedDuas" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "SavedSunnahDuas" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "UserUsages" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "SubscriptionOwnerships" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "DuaFeedPosts" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "DuaFeedLikes" ENABLE ROW LEVEL SECURITY;

-- Users
DROP POLICY IF EXISTS "users_select_own" ON "Users";
CREATE POLICY "users_select_own" ON "Users"
    FOR SELECT USING (auth.uid() = "AuthUserId");

DROP POLICY IF EXISTS "users_insert_own" ON "Users";
CREATE POLICY "users_insert_own" ON "Users"
    FOR INSERT WITH CHECK (auth.uid() = "AuthUserId");

DROP POLICY IF EXISTS "users_update_own" ON "Users";
CREATE POLICY "users_update_own" ON "Users"
    FOR UPDATE USING (auth.uid() = "AuthUserId");

-- SavedDuas
DROP POLICY IF EXISTS "saved_duas_own" ON "SavedDuas";
CREATE POLICY "saved_duas_own" ON "SavedDuas"
    FOR ALL
    USING (
        "UserId" IN (SELECT "UserId" FROM "Users" WHERE "AuthUserId" = auth.uid())
    )
    WITH CHECK (
        "UserId" IN (SELECT "UserId" FROM "Users" WHERE "AuthUserId" = auth.uid())
    );

-- SavedSunnahDuas
DROP POLICY IF EXISTS "saved_sunnah_duas_own" ON "SavedSunnahDuas";
CREATE POLICY "saved_sunnah_duas_own" ON "SavedSunnahDuas"
    FOR ALL
    USING (
        "UserId" IN (SELECT "UserId" FROM "Users" WHERE "AuthUserId" = auth.uid())
    )
    WITH CHECK (
        "UserId" IN (SELECT "UserId" FROM "Users" WHERE "AuthUserId" = auth.uid())
    );

-- UserUsages
DROP POLICY IF EXISTS "user_usages_select_own" ON "UserUsages";
CREATE POLICY "user_usages_select_own" ON "UserUsages"
    FOR SELECT USING (
        "UserId" IN (SELECT "UserId" FROM "Users" WHERE "AuthUserId" = auth.uid())
    );

DROP POLICY IF EXISTS "user_usages_insert_own" ON "UserUsages";
CREATE POLICY "user_usages_insert_own" ON "UserUsages"
    FOR INSERT WITH CHECK (
        "UserId" IN (SELECT "UserId" FROM "Users" WHERE "AuthUserId" = auth.uid())
    );

DROP POLICY IF EXISTS "user_usages_update_own" ON "UserUsages";
CREATE POLICY "user_usages_update_own" ON "UserUsages"
    FOR UPDATE USING (
        "UserId" IN (SELECT "UserId" FROM "Users" WHERE "AuthUserId" = auth.uid())
    );

-- SubscriptionOwnerships
DROP POLICY IF EXISTS "subscription_ownerships_select_own" ON "SubscriptionOwnerships";
CREATE POLICY "subscription_ownerships_select_own" ON "SubscriptionOwnerships"
    FOR SELECT USING (
        "UserId" IN (SELECT "UserId" FROM "Users" WHERE "AuthUserId" = auth.uid())
    );

-- DuaFeedPosts (public read for active posts; authors manage their own)
DROP POLICY IF EXISTS "dua_feed_posts_select_active" ON "DuaFeedPosts";
CREATE POLICY "dua_feed_posts_select_active" ON "DuaFeedPosts"
    FOR SELECT USING ("ExpiresAt" > now());

DROP POLICY IF EXISTS "dua_feed_posts_insert_own" ON "DuaFeedPosts";
CREATE POLICY "dua_feed_posts_insert_own" ON "DuaFeedPosts"
    FOR INSERT WITH CHECK (
        "UserId" IN (SELECT "UserId" FROM "Users" WHERE "AuthUserId" = auth.uid())
    );

DROP POLICY IF EXISTS "dua_feed_posts_delete_own" ON "DuaFeedPosts";
CREATE POLICY "dua_feed_posts_delete_own" ON "DuaFeedPosts"
    FOR DELETE USING (
        "UserId" IN (SELECT "UserId" FROM "Users" WHERE "AuthUserId" = auth.uid())
    );

-- DuaFeedLikes (any authenticated user can like; users manage their own likes)
DROP POLICY IF EXISTS "dua_feed_likes_select_all" ON "DuaFeedLikes";
CREATE POLICY "dua_feed_likes_select_all" ON "DuaFeedLikes"
    FOR SELECT USING (true);

DROP POLICY IF EXISTS "dua_feed_likes_insert_own" ON "DuaFeedLikes";
CREATE POLICY "dua_feed_likes_insert_own" ON "DuaFeedLikes"
    FOR INSERT WITH CHECK (
        "UserId" IN (SELECT "UserId" FROM "Users" WHERE "AuthUserId" = auth.uid())
    );

DROP POLICY IF EXISTS "dua_feed_likes_delete_own" ON "DuaFeedLikes";
CREATE POLICY "dua_feed_likes_delete_own" ON "DuaFeedLikes"
    FOR DELETE USING (
        "UserId" IN (SELECT "UserId" FROM "Users" WHERE "AuthUserId" = auth.uid())
    );
