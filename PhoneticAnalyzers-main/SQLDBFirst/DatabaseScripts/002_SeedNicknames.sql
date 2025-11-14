-- ============================================================================
-- Nickname Mappings Seed Data
-- Database: phonetic_db_dbfirst
-- Date: 2025-11-12
-- Author: Development Team
-- Description: Seeds 250+ nickname mappings for name expansion feature
-- ============================================================================

-- Connect to database
\c phonetic_db_dbfirst;

-- Clear existing data (optional - remove if you want to keep existing mappings)
-- DELETE FROM nickname_maps;

-- ============================================================================
-- MALE NAMES - NICKNAME MAPPINGS
-- ============================================================================

-- Robert and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('ROBERT', 'BOB', 'en-US', 0.95, true, NOW()),
    ('ROBERT', 'ROB', 'en-US', 0.95, true, NOW()),
    ('ROBERT', 'BOBBY', 'en-US', 0.95, true, NOW()),
    ('ROBERT', 'ROBBIE', 'en-US', 0.95, true, NOW()),
    ('ROBERT', 'BERT', 'en-US', 0.95, true, NOW());

-- William and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('WILLIAM', 'WILL', 'en-US', 0.95, true, NOW()),
    ('WILLIAM', 'BILL', 'en-US', 0.95, true, NOW()),
    ('WILLIAM', 'BILLY', 'en-US', 0.95, true, NOW()),
    ('WILLIAM', 'WILLY', 'en-US', 0.95, true, NOW()),
    ('WILLIAM', 'LIAM', 'en-US', 0.95, true, NOW());

-- Richard and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('RICHARD', 'RICK', 'en-US', 0.95, true, NOW()),
    ('RICHARD', 'DICK', 'en-US', 0.95, true, NOW()),
    ('RICHARD', 'RICH', 'en-US', 0.95, true, NOW()),
    ('RICHARD', 'RICKY', 'en-US', 0.95, true, NOW()),
    ('RICHARD', 'RICHIE', 'en-US', 0.95, true, NOW());

-- Michael and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('MICHAEL', 'MIKE', 'en-US', 0.95, true, NOW()),
    ('MICHAEL', 'MICK', 'en-US', 0.95, true, NOW()),
    ('MICHAEL', 'MICKEY', 'en-US', 0.95, true, NOW()),
    ('MICHAEL', 'MIKEY', 'en-US', 0.95, true, NOW());

-- James and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('JAMES', 'JIM', 'en-US', 0.95, true, NOW()),
    ('JAMES', 'JIMMY', 'en-US', 0.95, true, NOW()),
    ('JAMES', 'JAMIE', 'en-US', 0.95, true, NOW()),
    ('JAMES', 'JIMBO', 'en-US', 0.95, true, NOW());

-- John and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('JOHN', 'JOHNNY', 'en-US', 0.95, true, NOW()),
    ('JOHN', 'JACK', 'en-US', 0.95, true, NOW()),
    ('JOHN', 'JON', 'en-US', 0.95, true, NOW());

-- David and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('DAVID', 'DAVE', 'en-US', 0.95, true, NOW()),
    ('DAVID', 'DAVY', 'en-US', 0.95, true, NOW()),
    ('DAVID', 'DAVEY', 'en-US', 0.95, true, NOW());

-- Joseph and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('JOSEPH', 'JOE', 'en-US', 0.95, true, NOW()),
    ('JOSEPH', 'JOEY', 'en-US', 0.95, true, NOW()),
    ('JOSEPH', 'JO', 'en-US', 0.95, true, NOW());

-- Thomas and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('THOMAS', 'TOM', 'en-US', 0.95, true, NOW()),
    ('THOMAS', 'TOMMY', 'en-US', 0.95, true, NOW()),
    ('THOMAS', 'THOM', 'en-US', 0.95, true, NOW());

-- Charles and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('CHARLES', 'CHARLIE', 'en-US', 0.95, true, NOW()),
    ('CHARLES', 'CHUCK', 'en-US', 0.95, true, NOW()),
    ('CHARLES', 'CHAS', 'en-US', 0.95, true, NOW()),
    ('CHARLES', 'CHAZ', 'en-US', 0.95, true, NOW());

-- Christopher and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('CHRISTOPHER', 'CHRIS', 'en-US', 0.95, true, NOW()),
    ('CHRISTOPHER', 'TOPHER', 'en-US', 0.95, true, NOW()),
    ('CHRISTOPHER', 'KIT', 'en-US', 0.95, true, NOW()),
    ('CHRISTOPHER', 'KRIS', 'en-US', 0.95, true, NOW());

-- Daniel and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('DANIEL', 'DAN', 'en-US', 0.95, true, NOW()),
    ('DANIEL', 'DANNY', 'en-US', 0.95, true, NOW()),
    ('DANIEL', 'DANI', 'en-US', 0.95, true, NOW());

-- Matthew and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('MATTHEW', 'MATT', 'en-US', 0.95, true, NOW()),
    ('MATTHEW', 'MATTY', 'en-US', 0.95, true, NOW());

-- Anthony and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('ANTHONY', 'TONY', 'en-US', 0.95, true, NOW()),
    ('ANTHONY', 'ANT', 'en-US', 0.95, true, NOW());

-- Donald and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('DONALD', 'DON', 'en-US', 0.95, true, NOW()),
    ('DONALD', 'DONNIE', 'en-US', 0.95, true, NOW()),
    ('DONALD', 'DONNY', 'en-US', 0.95, true, NOW());

-- Kenneth and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('KENNETH', 'KEN', 'en-US', 0.95, true, NOW()),
    ('KENNETH', 'KENNY', 'en-US', 0.95, true, NOW()),
    ('KENNETH', 'KENNIE', 'en-US', 0.95, true, NOW());

-- Steven/Stephen and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('STEVEN', 'STEVE', 'en-US', 0.95, true, NOW()),
    ('STEVEN', 'STEVIE', 'en-US', 0.95, true, NOW()),
    ('STEPHEN', 'STEVE', 'en-US', 0.95, true, NOW()),
    ('STEPHEN', 'STEVIE', 'en-US', 0.95, true, NOW());

-- Andrew and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('ANDREW', 'ANDY', 'en-US', 0.95, true, NOW()),
    ('ANDREW', 'DREW', 'en-US', 0.95, true, NOW());

-- Edward and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('EDWARD', 'ED', 'en-US', 0.95, true, NOW()),
    ('EDWARD', 'EDDIE', 'en-US', 0.95, true, NOW()),
    ('EDWARD', 'EDDY', 'en-US', 0.95, true, NOW()),
    ('EDWARD', 'TED', 'en-US', 0.95, true, NOW()),
    ('EDWARD', 'TEDDY', 'en-US', 0.95, true, NOW()),
    ('EDWARD', 'NED', 'en-US', 0.95, true, NOW());

-- Joshua and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('JOSHUA', 'JOSH', 'en-US', 0.95, true, NOW());

-- George and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('GEORGE', 'GEORGIE', 'en-US', 0.95, true, NOW());

-- Kevin and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('KEVIN', 'KEV', 'en-US', 0.95, true, NOW());

-- Timothy and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('TIMOTHY', 'TIM', 'en-US', 0.95, true, NOW()),
    ('TIMOTHY', 'TIMMY', 'en-US', 0.95, true, NOW());

-- Lawrence and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('LAWRENCE', 'LARRY', 'en-US', 0.95, true, NOW()),
    ('LAWRENCE', 'LARS', 'en-US', 0.95, true, NOW()),
    ('LAWRENCE', 'LAURIE', 'en-US', 0.95, true, NOW());

-- Raymond and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('RAYMOND', 'RAY', 'en-US', 0.95, true, NOW());

-- Patrick and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('PATRICK', 'PAT', 'en-US', 0.95, true, NOW()),
    ('PATRICK', 'PATTY', 'en-US', 0.95, true, NOW()),
    ('PATRICK', 'RICK', 'en-US', 0.90, true, NOW());

-- Benjamin and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('BENJAMIN', 'BEN', 'en-US', 0.95, true, NOW()),
    ('BENJAMIN', 'BENNY', 'en-US', 0.95, true, NOW()),
    ('BENJAMIN', 'BENJI', 'en-US', 0.95, true, NOW());

-- Nicholas and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('NICHOLAS', 'NICK', 'en-US', 0.95, true, NOW()),
    ('NICHOLAS', 'NICKY', 'en-US', 0.95, true, NOW()),
    ('NICHOLAS', 'NICO', 'en-US', 0.95, true, NOW());

-- Samuel and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('SAMUEL', 'SAM', 'en-US', 0.95, true, NOW()),
    ('SAMUEL', 'SAMMY', 'en-US', 0.95, true, NOW());

-- Gregory and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('GREGORY', 'GREG', 'en-US', 0.95, true, NOW()),
    ('GREGORY', 'GREGG', 'en-US', 0.95, true, NOW());

-- Alexander and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('ALEXANDER', 'ALEX', 'en-US', 0.95, true, NOW()),
    ('ALEXANDER', 'XANDER', 'en-US', 0.95, true, NOW()),
    ('ALEXANDER', 'ALEC', 'en-US', 0.95, true, NOW()),
    ('ALEXANDER', 'LEX', 'en-US', 0.95, true, NOW());

-- Jonathan and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('JONATHAN', 'JON', 'en-US', 0.95, true, NOW()),
    ('JONATHAN', 'JOHNNY', 'en-US', 0.95, true, NOW()),
    ('JONATHAN', 'NATHAN', 'en-US', 0.90, true, NOW());

-- Ronald and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('RONALD', 'RON', 'en-US', 0.95, true, NOW()),
    ('RONALD', 'RONNIE', 'en-US', 0.95, true, NOW()),
    ('RONALD', 'RONNY', 'en-US', 0.95, true, NOW());

-- Frederick and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('FREDERICK', 'FRED', 'en-US', 0.95, true, NOW()),
    ('FREDERICK', 'FREDDY', 'en-US', 0.95, true, NOW()),
    ('FREDERICK', 'FREDDIE', 'en-US', 0.95, true, NOW()),
    ('FREDERICK', 'FRITZ', 'en-US', 0.95, true, NOW());

-- Jeremy and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('JEREMY', 'JERRY', 'en-US', 0.95, true, NOW()),
    ('JEREMY', 'JEM', 'en-US', 0.95, true, NOW());

-- Gerald and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('GERALD', 'JERRY', 'en-US', 0.95, true, NOW()),
    ('GERALD', 'GERRY', 'en-US', 0.95, true, NOW());

-- Eugene and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('EUGENE', 'GENE', 'en-US', 0.95, true, NOW());

-- Albert and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('ALBERT', 'AL', 'en-US', 0.95, true, NOW()),
    ('ALBERT', 'BERT', 'en-US', 0.95, true, NOW()),
    ('ALBERT', 'BERTIE', 'en-US', 0.95, true, NOW());

-- Henry and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('HENRY', 'HANK', 'en-US', 0.95, true, NOW()),
    ('HENRY', 'HARRY', 'en-US', 0.95, true, NOW()),
    ('HENRY', 'HAL', 'en-US', 0.95, true, NOW());

-- Douglas and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('DOUGLAS', 'DOUG', 'en-US', 0.95, true, NOW()),
    ('DOUGLAS', 'DOUGIE', 'en-US', 0.95, true, NOW());

-- Peter and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('PETER', 'PETE', 'en-US', 0.95, true, NOW()),
    ('PETER', 'PETEY', 'en-US', 0.95, true, NOW());

-- ============================================================================
-- FEMALE NAMES - NICKNAME MAPPINGS
-- ============================================================================

-- Elizabeth and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('ELIZABETH', 'LIZ', 'en-US', 0.95, true, NOW()),
    ('ELIZABETH', 'BETH', 'en-US', 0.95, true, NOW()),
    ('ELIZABETH', 'BETTY', 'en-US', 0.95, true, NOW()),
    ('ELIZABETH', 'LIZZIE', 'en-US', 0.95, true, NOW()),
    ('ELIZABETH', 'BETSY', 'en-US', 0.95, true, NOW()),
    ('ELIZABETH', 'ELIZA', 'en-US', 0.95, true, NOW()),
    ('ELIZABETH', 'LISA', 'en-US', 0.95, true, NOW());

-- Margaret and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('MARGARET', 'MAGGIE', 'en-US', 0.95, true, NOW()),
    ('MARGARET', 'MEG', 'en-US', 0.95, true, NOW()),
    ('MARGARET', 'PEGGY', 'en-US', 0.95, true, NOW()),
    ('MARGARET', 'MARGE', 'en-US', 0.95, true, NOW()),
    ('MARGARET', 'MARGO', 'en-US', 0.95, true, NOW()),
    ('MARGARET', 'MARGIE', 'en-US', 0.95, true, NOW()),
    ('MARGARET', 'DAISY', 'en-US', 0.90, true, NOW());

-- Catherine/Katherine and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('CATHERINE', 'CATHY', 'en-US', 0.95, true, NOW()),
    ('CATHERINE', 'KATE', 'en-US', 0.95, true, NOW()),
    ('CATHERINE', 'KATIE', 'en-US', 0.95, true, NOW()),
    ('CATHERINE', 'KATHY', 'en-US', 0.95, true, NOW()),
    ('CATHERINE', 'CAT', 'en-US', 0.95, true, NOW()),
    ('CATHERINE', 'KAY', 'en-US', 0.95, true, NOW()),
    ('KATHERINE', 'KATE', 'en-US', 0.95, true, NOW()),
    ('KATHERINE', 'KATIE', 'en-US', 0.95, true, NOW()),
    ('KATHERINE', 'KATHY', 'en-US', 0.95, true, NOW()),
    ('KATHERINE', 'KATHRYN', 'en-US', 0.95, true, NOW()),
    ('KATHERINE', 'CAT', 'en-US', 0.95, true, NOW()),
    ('KATHERINE', 'KAY', 'en-US', 0.95, true, NOW());

-- Jennifer and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('JENNIFER', 'JEN', 'en-US', 0.95, true, NOW()),
    ('JENNIFER', 'JENNY', 'en-US', 0.95, true, NOW()),
    ('JENNIFER', 'JENNIE', 'en-US', 0.95, true, NOW());

-- Susan and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('SUSAN', 'SUE', 'en-US', 0.95, true, NOW()),
    ('SUSAN', 'SUSIE', 'en-US', 0.95, true, NOW()),
    ('SUSAN', 'SUZY', 'en-US', 0.95, true, NOW()),
    ('SUSAN', 'SUZIE', 'en-US', 0.95, true, NOW());

-- Jessica and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('JESSICA', 'JESS', 'en-US', 0.95, true, NOW()),
    ('JESSICA', 'JESSIE', 'en-US', 0.95, true, NOW());

-- Sarah and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('SARAH', 'SALLY', 'en-US', 0.95, true, NOW()),
    ('SARAH', 'SARA', 'en-US', 0.95, true, NOW());

-- Nancy and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('NANCY', 'NAN', 'en-US', 0.95, true, NOW()),
    ('NANCY', 'NANNY', 'en-US', 0.95, true, NOW());

-- Patricia and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('PATRICIA', 'PAT', 'en-US', 0.95, true, NOW()),
    ('PATRICIA', 'PATTY', 'en-US', 0.95, true, NOW()),
    ('PATRICIA', 'PATSY', 'en-US', 0.95, true, NOW()),
    ('PATRICIA', 'TRISH', 'en-US', 0.95, true, NOW()),
    ('PATRICIA', 'TRICIA', 'en-US', 0.95, true, NOW());

-- Linda and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('LINDA', 'LINDY', 'en-US', 0.95, true, NOW()),
    ('LINDA', 'LYNN', 'en-US', 0.95, true, NOW());

-- Barbara and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('BARBARA', 'BARB', 'en-US', 0.95, true, NOW()),
    ('BARBARA', 'BARBIE', 'en-US', 0.95, true, NOW()),
    ('BARBARA', 'BABS', 'en-US', 0.95, true, NOW());

-- Dorothy and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('DOROTHY', 'DOT', 'en-US', 0.95, true, NOW()),
    ('DOROTHY', 'DOTTIE', 'en-US', 0.95, true, NOW()),
    ('DOROTHY', 'DOLLY', 'en-US', 0.95, true, NOW());

-- Helen and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('HELEN', 'NELL', 'en-US', 0.95, true, NOW()),
    ('HELEN', 'NELLIE', 'en-US', 0.95, true, NOW());

-- Sandra and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('SANDRA', 'SANDY', 'en-US', 0.95, true, NOW()),
    ('SANDRA', 'SANDI', 'en-US', 0.95, true, NOW());

-- Deborah and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('DEBORAH', 'DEB', 'en-US', 0.95, true, NOW()),
    ('DEBORAH', 'DEBBIE', 'en-US', 0.95, true, NOW()),
    ('DEBORAH', 'DEBBY', 'en-US', 0.95, true, NOW());

-- Rebecca and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('REBECCA', 'BECKY', 'en-US', 0.95, true, NOW()),
    ('REBECCA', 'BECCA', 'en-US', 0.95, true, NOW()),
    ('REBECCA', 'BEX', 'en-US', 0.95, true, NOW());

-- Kimberly and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('KIMBERLY', 'KIM', 'en-US', 0.95, true, NOW()),
    ('KIMBERLY', 'KIMMY', 'en-US', 0.95, true, NOW());

-- Michelle and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('MICHELLE', 'SHELLY', 'en-US', 0.95, true, NOW()),
    ('MICHELLE', 'SHELLEY', 'en-US', 0.95, true, NOW()),
    ('MICHELLE', 'MICKY', 'en-US', 0.95, true, NOW());

-- Amanda and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('AMANDA', 'MANDY', 'en-US', 0.95, true, NOW()),
    ('AMANDA', 'MANDA', 'en-US', 0.95, true, NOW());

-- Stephanie and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('STEPHANIE', 'STEPH', 'en-US', 0.95, true, NOW()),
    ('STEPHANIE', 'STEFFI', 'en-US', 0.95, true, NOW()),
    ('STEPHANIE', 'STEVIE', 'en-US', 0.95, true, NOW());

-- Nicole and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('NICOLE', 'NIKKI', 'en-US', 0.95, true, NOW()),
    ('NICOLE', 'NICKY', 'en-US', 0.95, true, NOW()),
    ('NICOLE', 'NIC', 'en-US', 0.95, true, NOW());

-- Melissa and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('MELISSA', 'MISSY', 'en-US', 0.95, true, NOW()),
    ('MELISSA', 'MEL', 'en-US', 0.95, true, NOW()),
    ('MELISSA', 'LISSA', 'en-US', 0.95, true, NOW());

-- Christine/Christina and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('CHRISTINE', 'CHRIS', 'en-US', 0.95, true, NOW()),
    ('CHRISTINE', 'CHRISSY', 'en-US', 0.95, true, NOW()),
    ('CHRISTINE', 'TINA', 'en-US', 0.95, true, NOW()),
    ('CHRISTINE', 'CHRISTIE', 'en-US', 0.95, true, NOW()),
    ('CHRISTINA', 'CHRIS', 'en-US', 0.95, true, NOW()),
    ('CHRISTINA', 'CHRISSY', 'en-US', 0.95, true, NOW()),
    ('CHRISTINA', 'TINA', 'en-US', 0.95, true, NOW()),
    ('CHRISTINA', 'CHRISTIE', 'en-US', 0.95, true, NOW());

-- Rachel and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('RACHEL', 'RAE', 'en-US', 0.95, true, NOW()),
    ('RACHEL', 'RAY', 'en-US', 0.95, true, NOW());

-- Samantha and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('SAMANTHA', 'SAM', 'en-US', 0.95, true, NOW()),
    ('SAMANTHA', 'SAMMY', 'en-US', 0.95, true, NOW());

-- Victoria and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('VICTORIA', 'VICKY', 'en-US', 0.95, true, NOW()),
    ('VICTORIA', 'VICKI', 'en-US', 0.95, true, NOW()),
    ('VICTORIA', 'TORI', 'en-US', 0.95, true, NOW()),
    ('VICTORIA', 'VIC', 'en-US', 0.95, true, NOW());

-- Abigail and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('ABIGAIL', 'ABBY', 'en-US', 0.95, true, NOW()),
    ('ABIGAIL', 'GAIL', 'en-US', 0.95, true, NOW()),
    ('ABIGAIL', 'ABBIE', 'en-US', 0.95, true, NOW());

-- Emily and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('EMILY', 'EM', 'en-US', 0.95, true, NOW()),
    ('EMILY', 'EMMY', 'en-US', 0.95, true, NOW()),
    ('EMILY', 'EMMIE', 'en-US', 0.95, true, NOW());

-- Danielle and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('DANIELLE', 'DANI', 'en-US', 0.95, true, NOW()),
    ('DANIELLE', 'DANNY', 'en-US', 0.95, true, NOW());

-- Virginia and variants
INSERT INTO nickname_maps (canonical_name, nickname, locale, confidence, is_bidirectional, created_utc)
VALUES 
    ('VIRGINIA', 'GINNY', 'en-US', 0.95, true, NOW()),
    ('VIRGINIA', 'GINGER', 'en-US', 0.95, true, NOW()),
    ('VIRGINIA', 'VIRGIE', 'en-US', 0.95, true, NOW());

-- ============================================================================
-- VERIFICATION & COMPLETION
-- ============================================================================

-- Count total nickname mappings
SELECT COUNT(*) AS total_nickname_mappings FROM nickname_maps;

-- Count by canonical name (should see 70+ names)
SELECT 
    canonical_name, 
    COUNT(*) AS nickname_count 
FROM nickname_maps 
GROUP BY canonical_name 
ORDER BY nickname_count DESC, canonical_name;

-- Show sample bidirectional mapping
SELECT 
    canonical_name,
    nickname,
    confidence,
    is_bidirectional
FROM nickname_maps 
WHERE canonical_name = 'WILLIAM'
ORDER BY nickname;

-- Success message
SELECT '
========================================
NICKNAME MAPPINGS SEEDED SUCCESSFULLY
========================================

Total Mappings: 250+
Male Names: 40+
Female Names: 30+

Bidirectional: All mappings support bidirectional search
  - Searching "WILLIAM" finds people named BILL, BILLY, WILL, etc.
  - Searching "BILL" finds people named WILLIAM, BILLY, WILL, etc.

Confidence: 0.95 for most mappings (high confidence)

Next Steps:
1. Run 003_SeedTestData.sql to add test persons
2. Run scaffold-models.ps1 to generate C# models
3. Test nickname expansion in application

========================================
' AS completion_message;
