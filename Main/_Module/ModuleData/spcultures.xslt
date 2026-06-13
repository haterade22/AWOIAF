<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<!-- Identity transformation - copies everything by default -->
	<xsl:output omit-xml-declaration="no" indent="yes"/>

	<xsl:template match="@*|node()">
		<xsl:copy>
			<xsl:apply-templates select="@*|node()"/>
		</xsl:copy>
	</xsl:template>

	<!--
	  GoT (Robert's Rebellion) display-rename of the 6 vanilla-base cultures (ADR-011).
	  ONLY the @name and @text display attributes are overridden; every other vanilla
	  attribute and child element passes through the identity transform untouched (per
	  .claude/rules/xslt.md). The runtime culture StringId stays vanilla
	  (sturgia/vlandia/empire/aserai/khuzait/battania) — see .claude/rules/xml-data.md.
	-->

	<!-- The North — House Stark (sturgia) -->
	<xsl:template match="Culture[@id='sturgia']/@name">
		<xsl:attribute name="name">{=dots_culture_north}The North</xsl:attribute>
	</xsl:template>
	<xsl:template match="Culture[@id='sturgia']/@text">
		<xsl:attribute name="text">{=dots_culture_north_desc}The hardy folk of the North, sworn to House Stark of Winterfell. Keepers of the old gods and the long winters, they muster grim shield-wall infantry across a realm larger than the six southern kingdoms combined.</xsl:attribute>
	</xsl:template>

	<!-- The Westerlands — House Lannister (vlandia) -->
	<xsl:template match="Culture[@id='vlandia']/@name">
		<xsl:attribute name="name">{=dots_culture_westerlands}The Westerlands</xsl:attribute>
	</xsl:template>
	<xsl:template match="Culture[@id='vlandia']/@text">
		<xsl:attribute name="text">{=dots_culture_westerlands_desc}The gold-rich Westerlands, ruled from Casterly Rock by House Lannister. Their mines fund the finest mailed knights and crossbowmen in Westeros — and a Lannister always pays his debts.</xsl:attribute>
	</xsl:template>

	<!-- The Reach — House Tyrell (empire) -->
	<xsl:template match="Culture[@id='empire']/@name">
		<xsl:attribute name="name">{=dots_culture_reach}The Reach</xsl:attribute>
	</xsl:template>
	<xsl:template match="Culture[@id='empire']/@text">
		<xsl:attribute name="text">{=dots_culture_reach_desc}The lush and populous Reach, governed from Highgarden by House Tyrell. The most chivalrous and prosperous of the kingdoms, it fields gallant knights, longbowmen, and the largest armies in the realm.</xsl:attribute>
	</xsl:template>

	<!-- Dorne — House Martell (aserai) -->
	<xsl:template match="Culture[@id='aserai']/@name">
		<xsl:attribute name="name">{=dots_culture_dorne}Dorne</xsl:attribute>
	</xsl:template>
	<xsl:template match="Culture[@id='aserai']/@text">
		<xsl:attribute name="text">{=dots_culture_dorne_desc}Sun-scorched Dorne, held from Sunspear by House Martell. Unbowed, unbent, unbroken, the Dornish wage war with ambush, spear, and poisoned arrow across deserts no invader has ever truly conquered.</xsl:attribute>
	</xsl:template>

	<!-- Dothraki &amp; Essos (khuzait) -->
	<xsl:template match="Culture[@id='khuzait']/@name">
		<xsl:attribute name="name">{=dots_culture_dothraki}Dothraki</xsl:attribute>
	</xsl:template>
	<xsl:template match="Culture[@id='khuzait']/@text">
		<xsl:attribute name="text">{=dots_culture_dothraki_desc}The horse-lords of the Dothraki sea and the sellsword companies of the Free Cities. Bound to no throne in Westeros, these riders and mercenaries answer only to strength and coin.</xsl:attribute>
	</xsl:template>

	<!-- The Free Folk (battania) -->
	<xsl:template match="Culture[@id='battania']/@name">
		<xsl:attribute name="name">{=dots_culture_freefolk}The Free Folk</xsl:attribute>
	</xsl:template>
	<xsl:template match="Culture[@id='battania']/@text">
		<xsl:attribute name="text">{=dots_culture_freefolk_desc}The free folk who dwell beyond the Wall, kneeling to no king. Wildling raiders, spearwives, and skin-changers who survive the haunted forest by cunning, ferocity, and the bow.</xsl:attribute>
	</xsl:template>

</xsl:stylesheet>
